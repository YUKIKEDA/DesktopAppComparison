# CPU % measurement via --cpu-bench phase signaling.
# Apps write phase names to a file; this script samples process CPU during each phase.
# Metric: % of all logical processors (sum of related processes for Electron/Tauri).
# Peak: max of idle/add/scroll/filter averages. 3 runs averaged.

$ErrorActionPreference = "Continue"
$Root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { "D:\home\Programs\DesktopAppComparison" }
$DataDir = Join-Path $Root "data"
$OutDir = Join-Path $Root "_tools"
$LogFile = Join-Path $OutDir "cpu_metrics_log.txt"
$JsonFile = Join-Path $OutDir "cpu_metrics_results.json"
$DataFile = Join-Path $DataDir "project_1000.json"
$Runs = 3
$Cores = [Environment]::ProcessorCount
$SampleMs = 200
$PhaseTimeoutSec = 90

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
"" | Set-Content $LogFile -Encoding utf8

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $msg
  Add-Content $LogFile $line -Encoding utf8
  Write-Host $line
}

function Stop-AppProcesses([string[]]$names, [string]$CommandLineMatch = $null) {
  foreach ($n in $names) {
    $procs = @(Get-Process -Name $n -ErrorAction SilentlyContinue)
    foreach ($p in $procs) {
      if ($CommandLineMatch) {
        try {
          $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($p.Id)" -EA SilentlyContinue).CommandLine
          if (-not $cmd -or ($cmd -notmatch $CommandLineMatch)) { continue }
        } catch { continue }
      }
      Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    }
  }
  Start-Sleep -Milliseconds 500
}

function Clear-Persist([string[]]$paths) {
  foreach ($p in $paths) {
    if ($p -match '\*') {
      Get-Item $p -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
      }
    } elseif (Test-Path -LiteralPath $p) {
      Remove-Item -LiteralPath $p -Force -ErrorAction SilentlyContinue
    }
  }
}

function Get-RelatedProcesses([hashtable]$App) {
  $procs = @()
  foreach ($n in $App.Processes) {
    if ($n -eq "msedgewebview2") { continue }
    $candidates = @(Get-Process -Name $n -ErrorAction SilentlyContinue)
    if ($App.KillMatch) {
      $filtered = @()
      foreach ($p in $candidates) {
        try {
          $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($p.Id)" -EA SilentlyContinue).CommandLine
          if ($cmd -and ($cmd -match $App.KillMatch)) { $filtered += $p }
        } catch {}
      }
      $procs += $filtered
    } else {
      $procs += $candidates
    }
  }
  # Tauri: include full process tree (WebView2 may not be a direct child)
  if ($App.Name -eq "Tauri") {
    $roots = @($procs | Where-Object { $_.ProcessName -eq "todoapp-tauri" })
    $queue = New-Object System.Collections.Generic.Queue[int]
    $seen = @{}
    foreach ($r in $roots) { $queue.Enqueue([int]$r.Id) }
    while ($queue.Count -gt 0) {
      $id = $queue.Dequeue()
      if ($seen.ContainsKey($id)) { continue }
      $seen[$id] = $true
      $p = Get-Process -Id $id -ErrorAction SilentlyContinue
      if ($p) { $procs += $p }
      Get-CimInstance Win32_Process -Filter "ParentProcessId=$id" -ErrorAction SilentlyContinue | ForEach-Object {
        $queue.Enqueue([int]$_.ProcessId)
      }
    }
  }
  return @($procs | Where-Object { $_ } | Sort-Object Id -Unique)
}

function Get-ProcessCpuSnapshot($App) {
  $procs = Get-RelatedProcesses $App
  if ($procs.Count -eq 0) { return $null }
  $cpuSec = 0.0
  foreach ($p in $procs) {
    try { $cpuSec += $p.TotalProcessorTime.TotalSeconds } catch {}
  }
  return @{
    CpuSec = $cpuSec
    Count  = $procs.Count
    At     = [datetime]::UtcNow
  }
}

function Measure-CpuPercentDuringPhase {
  param(
    [string]$PhaseFile,
    [string]$ExpectedPhase,
    $App,
    [int]$TimeoutSec = 90
  )

  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  # Wait until phase starts
  while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $PhaseFile) {
      $cur = (Get-Content -LiteralPath $PhaseFile -Raw -ErrorAction SilentlyContinue).Trim()
      if ($cur -eq $ExpectedPhase) { break }
      if ($cur -eq "done" -or $cur -eq "error") { return $null }
    }
    Start-Sleep -Milliseconds 50
  }

  $samples = @()
  $prev = Get-ProcessCpuSnapshot $App
  while ((Get-Date) -lt $deadline) {
    if (-not (Test-Path -LiteralPath $PhaseFile)) { break }
    $cur = (Get-Content -LiteralPath $PhaseFile -Raw -ErrorAction SilentlyContinue).Trim()
    if ($cur -ne $ExpectedPhase) { break }

    Start-Sleep -Milliseconds $SampleMs
    $next = Get-ProcessCpuSnapshot $App
    if ($null -ne $prev -and $null -ne $next) {
      $wall = ($next.At - $prev.At).TotalSeconds
      if ($wall -gt 0) {
        $pct = (($next.CpuSec - $prev.CpuSec) / $wall / $Cores) * 100.0
        if ($pct -lt 0) { $pct = 0 }
        if ($pct -gt 100) { $pct = 100 }
        $samples += $pct
      }
    }
    $prev = $next
  }

  if ($samples.Count -eq 0) { return $null }
  return [math]::Round((($samples | Measure-Object -Average).Average), 1)
}

function Wait-Phase([string]$PhaseFile, [string]$Name, [int]$TimeoutSec = 90) {
  $deadline = (Get-Date).AddSeconds($TimeoutSec)
  while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $PhaseFile) {
      $cur = (Get-Content -LiteralPath $PhaseFile -Raw -ErrorAction SilentlyContinue).Trim()
      if ($cur -eq $Name) { return $true }
    }
    Start-Sleep -Milliseconds 50
  }
  return $false
}

function Measure-AppCpu {
  param($App)

  $idleRuns = @(); $addRuns = @(); $scrollRuns = @(); $filterRuns = @()

  for ($i = 1; $i -le $Runs; $i++) {
    Stop-AppProcesses $App.Processes $App.KillMatch
    if ($App.ExtraKill) { Stop-AppProcesses $App.ExtraKill }
    Clear-Persist $App.Persist

    $phaseFile = Join-Path $OutDir ("cpu_phase_{0}_{1}.txt" -f $App.Name, $i)
    if (Test-Path $phaseFile) { Remove-Item $phaseFile -Force }
    "boot" | Set-Content -LiteralPath $phaseFile -Encoding ascii

    Log ("{0} run {1}/{2}" -f $App.Name, $i, $Runs)

    $argList = @("--cpu-bench", "--cpu-bench-phase=$phaseFile", $DataFile)

    if ($App.Name -eq "WinUI3") {
      # AppsFolder cannot pass argv; drop request file into packaged LocalState then activate.
      $reg = Join-Path $PSScriptRoot "register_winui.ps1"
      & $reg
      $pfn = "d58fe1bb-f479-4f19-b358-06dee335f74c_k6bmzwkfnste6"
      $reqDir = Join-Path $env:LOCALAPPDATA "Packages\$pfn\LocalState"
      New-Item -ItemType Directory -Force -Path $reqDir | Out-Null
      $req = Join-Path $reqDir "cpu_bench_request.txt"
      @(
        "phase=$phaseFile"
        "json=$DataFile"
      ) | Set-Content -LiteralPath $req -Encoding ascii
      & $reg -Launch
    } elseif ($App.ExeArgsPrefix) {
      $wd = if ($App.WorkingDirectory) { $App.WorkingDirectory } else { Split-Path $App.Exe -Parent }
      $fullArgs = @($App.ExeArgsPrefix) + $argList
      $null = Start-Process -FilePath $App.Exe -ArgumentList $fullArgs -WorkingDirectory $wd -PassThru -WindowStyle Normal
    } else {
      if (-not (Test-Path $App.Exe)) { throw "Missing exe: $($App.Exe)" }
      $pInfo = @{
        FilePath         = $App.Exe
        ArgumentList     = $argList
        PassThru         = $true
        WindowStyle      = "Normal"
        WorkingDirectory = (Split-Path $App.Exe -Parent)
      }
      $null = Start-Process @pInfo
    }

    if (-not (Wait-Phase $phaseFile "idle" $PhaseTimeoutSec)) {
      Log ("{0} TIMEOUT waiting idle" -f $App.Name)
      Stop-AppProcesses $App.Processes $App.KillMatch
      throw "timeout idle"
    }

    $idle = Measure-CpuPercentDuringPhase $phaseFile "idle" $App $PhaseTimeoutSec
    $add = Measure-CpuPercentDuringPhase $phaseFile "add" $App $PhaseTimeoutSec
    $scroll = Measure-CpuPercentDuringPhase $phaseFile "scroll" $App $PhaseTimeoutSec
    $filter = Measure-CpuPercentDuringPhase $phaseFile "filter" $App $PhaseTimeoutSec

    $null = Wait-Phase $phaseFile "done" 30

    Log ("{0} idle={1} add={2} scroll={3} filter={4}" -f $App.Name, $idle, $add, $scroll, $filter)

    if ($null -ne $idle) { $idleRuns += $idle }
    if ($null -ne $add) { $addRuns += $add }
    if ($null -ne $scroll) { $scrollRuns += $scroll }
    if ($null -ne $filter) { $filterRuns += $filter }

    Stop-AppProcesses $App.Processes $App.KillMatch
    if ($App.ExtraKill) { Stop-AppProcesses $App.ExtraKill }
    Start-Sleep -Milliseconds 800
  }

  function Avg($arr) {
    if (-not $arr -or $arr.Count -eq 0) { return $null }
    return [math]::Round((($arr | Measure-Object -Average).Average), 1)
  }

  $idleA = Avg $idleRuns
  $addA = Avg $addRuns
  $scrollA = Avg $scrollRuns
  $filterA = Avg $filterRuns
  $vals = @($idleA, $addA, $scrollA, $filterA) | Where-Object { $null -ne $_ }
  $peak = if ($vals.Count) { ($vals | Measure-Object -Maximum).Maximum } else { $null }

  return [ordered]@{
    name         = $App.Name
    skipped      = $false
    idle         = $idleA
    add          = $addA
    scroll       = $scrollA
    filtering    = $filterA
    peak         = $peak
    idle_runs    = $idleRuns
    add_runs     = $addRuns
    scroll_runs  = $scrollRuns
    filter_runs  = $filterRuns
  }
}

$apps = @(
  @{
    Name = "Avalonia"
    Exe = Join-Path $Root "ToDoApp.Avalonia\bin\Release\net9.0\win-x64\ToDoApp.Avalonia.exe"
    Processes = @("ToDoApp.Avalonia")
    Persist = @((Join-Path $env:APPDATA "ToDoApp.Avalonia\data\project.json"))
  }
  @{
    Name = "Compose"
    Exe = Join-Path $Root "ToDoApp.KotlinMultiplatform\composeApp\build\compose\binaries\main-release\app\com.example.todoappkotlinmultiplatform\com.example.todoappkotlinmultiplatform.exe"
    Processes = @("com.example.todoappkotlinmultiplatform")
    Persist = @((Join-Path $env:USERPROFILE ".todoapp.kotlinmultiplatform\data\project.json"))
  }
  @{
    Name = "Electron"
    Exe = Join-Path $Root "ToDoApp.Electron\release-measure\win-unpacked\Todo App.exe"
    Processes = @("Todo App")
    Persist = @(
      (Join-Path $env:APPDATA "Todo App\data\project.json"),
      (Join-Path $env:APPDATA "todoapp-electron\data\project.json")
    )
  }
  @{
    Name = "Flutter"
    Exe = Join-Path $Root "todoapp_flutter\build\windows\x64\runner\Release\todoapp_flutter.exe"
    Processes = @("todoapp_flutter")
    Persist = @((Join-Path $env:USERPROFILE "Documents\todoapp_flutter\data\project.json"))
  }
  @{
    Name = "Tauri"
    Exe = Join-Path $Root "ToDoApp.Tauri\src-tauri\target\release\todoapp-tauri.exe"
    Processes = @("todoapp-tauri")
    Persist = @((Join-Path $env:APPDATA "com.yuuuu.todoapp-tauri\project.json"))
  }
  @{
    Name = "WPF"
    Exe = Join-Path $Root "ToDoApp.Wpf\bin\Release\net8.0-windows\win-x64\ToDoApp.Wpf.exe"
    Processes = @("ToDoApp.Wpf")
    Persist = @((Join-Path $env:APPDATA "ToDoApp.Wpf\data\project.json"))
  }
  @{
    Name = "WinUI3"
    Exe = Join-Path $Root "ToDoApp.WinUI\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\ToDoApp.WinUI.exe"
    Processes = @("ToDoApp.WinUI")
    Persist = @(
      (Join-Path $env:LOCALAPPDATA "ToDoApp.WinUI\Data\project.json"),
      (Join-Path $env:LOCALAPPDATA "Packages\d58fe1bb*\LocalState\Data\project.json")
    )
  }
  @{
    Name = "wxWidgets"
    # Prefer source run so --cpu-bench is available without Nuitka rebuild.
    Exe = "uv"
    ExeArgsPrefix = @("run", "python", "main.py")
    WorkingDirectory = Join-Path $Root "ToDoApp.wxWidgets"
    Processes = @("python", "pythonw")
    KillMatch = "ToDoApp\.wxWidgets|todoapp\.wxwidgets|main\.py"
    Persist = @((Join-Path $env:APPDATA "TodoApp.wxWidgets\data\project.json"))
  }
)

Log "ROOT=$Root cores=$Cores data=$DataFile"
$results = @()

foreach ($app in $apps) {
  $missing = $false
  if ($app.Name -eq "WinUI3") {
    $manifest = Join-Path $Root "ToDoApp.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\AppxManifest.xml"
    if (-not (Test-Path $manifest)) { $missing = $true; $missPath = $manifest }
  } elseif ($app.ExeArgsPrefix) {
    # uv/python path — skip Test-Path on Exe name
    $missing = $false
  } elseif (-not (Test-Path $app.Exe)) {
    $missing = $true; $missPath = $app.Exe
  }

  if ($missing) {
    Log ("SKIP {0}: missing {1}" -f $app.Name, $missPath)
    $results += [pscustomobject]@{ name = $app.Name; skipped = $true; reason = "missing exe" }
    continue
  }

  Log ("===== {0} =====" -f $app.Name)
  try {
    $row = Measure-AppCpu $app
    $results += [pscustomobject]$row
    Log ("SUMMARY {0}: idle={1} add={2} scroll={3} filter={4} peak={5}" -f `
      $row.name, $row.idle, $row.add, $row.scroll, $row.filtering, $row.peak)
  } catch {
    Log ("FAIL {0}: {1}" -f $app.Name, $_)
    $results += [pscustomobject]@{ name = $app.Name; skipped = $true; reason = "$_" }
  }
}

$results | ConvertTo-Json -Depth 6 | Set-Content $JsonFile -Encoding utf8
Log "DONE wrote $JsonFile"

# Always unregister WinUI leftovers after the suite
$reg = Join-Path $PSScriptRoot "register_winui.ps1"
if (Test-Path $reg) {
  try { & $reg -Unregister } catch {}
}

$cleanup = Join-Path $PSScriptRoot "cleanup_local_leftovers.ps1"
if (Test-Path $cleanup) {
  Log "Running cleanup_local_leftovers.ps1"
  & $cleanup
}
