# UI responsiveness measurement via --ui-bench.
# Each app writes a JSON result file; this script averages 3 runs.
#
# Metrics (written by apps):
#   startup_s          - process start → first interactive UI frame (empty project)
#   render_1000_s      - begin import of project_1000.json → list UI updated
#   scroll_fps         - average FPS over ~3s of programmatic scroll/load-more with 1000 items
#   filter_response_ms - average of 10 filter apply cycles (set → applied)
#
# Args passed to apps:
#   --ui-bench --ui-bench-out=<result.json> <optional ignored; apps load data/project_1000.json themselves via path arg>
#   We also pass the absolute path to data/project_1000.json as a plain arg for import during render_1000.

$ErrorActionPreference = "Continue"
$Root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { "D:\home\Programs\DesktopAppComparison" }
$DataDir = Join-Path $Root "data"
$OutDir = Join-Path $Root "_tools"
$LogFile = Join-Path $OutDir "ui_metrics_log.txt"
$JsonFile = Join-Path $OutDir "ui_metrics_results.json"
$Data1000 = Join-Path $DataDir "project_1000.json"
$Runs = 3
$TimeoutSec = 120

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
  # Tauri process tree
  $tauri = @(Get-Process -Name "todoapp-tauri" -EA SilentlyContinue)
  foreach ($r in $tauri) {
    $q = New-Object System.Collections.Generic.Queue[int]
    $q.Enqueue([int]$r.Id)
    $seen = @{}
    while ($q.Count -gt 0) {
      $id = $q.Dequeue()
      if ($seen.ContainsKey($id)) { continue }
      $seen[$id] = $true
      Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
      Get-CimInstance Win32_Process -Filter "ParentProcessId=$id" -EA SilentlyContinue | ForEach-Object {
        $q.Enqueue([int]$_.ProcessId)
      }
    }
  }
  Start-Sleep -Milliseconds 600
}

function Wait-ResultFile([string]$path, [int]$timeoutSec) {
  $deadline = (Get-Date).AddSeconds($timeoutSec)
  while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $path) {
      try {
        $raw = Get-Content -LiteralPath $path -Raw -Encoding utf8
        $obj = $raw | ConvertFrom-Json
        if ($null -ne $obj.startup_s) { return $obj }
      } catch {}
    }
    Start-Sleep -Milliseconds 200
  }
  return $null
}

function Measure-AppUi($App) {
  $startup = @(); $render = @(); $fps = @(); $filter = @()

  for ($i = 1; $i -le $Runs; $i++) {
    Stop-AppProcesses $App.Processes $App.KillMatch

    $resultPath = Join-Path $OutDir ("ui_result_{0}_{1}.json" -f $App.Name, $i)
    if (Test-Path $resultPath) { Remove-Item $resultPath -Force }

    Log ("{0} run {1}/{2}" -f $App.Name, $i, $Runs)

    $argList = @("--ui-bench", "--ui-bench-out=$resultPath", $Data1000)
    $obj = $null

    if ($App.Name -eq "WinUI3") {
      $reg = Join-Path $PSScriptRoot "register_winui.ps1"
      & $reg
      $pfn = "d58fe1bb-f479-4f19-b358-06dee335f74c_k6bmzwkfnste6"
      $reqDir = Join-Path $env:LOCALAPPDATA "Packages\$pfn\LocalState"
      New-Item -ItemType Directory -Force -Path $reqDir | Out-Null
      Remove-Item (Join-Path $reqDir "cpu_bench_request.txt") -Force -EA SilentlyContinue
      $localOut = Join-Path $reqDir "ui_bench_result.json"
      $localJson = Join-Path $reqDir "project_1000.json"
      Copy-Item -LiteralPath $Data1000 -Destination $localJson -Force
      if (Test-Path $localOut) { Remove-Item $localOut -Force }
      $req = Join-Path $reqDir "ui_bench_request.txt"
      @(
        "out=$localOut"
        "json=$localJson"
      ) | Set-Content -LiteralPath $req -Encoding ascii
      & $reg -Launch
      $obj = Wait-ResultFile $localOut $TimeoutSec
      if ($null -ne $obj) {
        ($obj | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $resultPath -Encoding utf8
      }
    } elseif ($App.ExeArgsPrefix) {
      $wd = $App.WorkingDirectory
      $fullArgs = @($App.ExeArgsPrefix) + $argList
      $null = Start-Process -FilePath $App.Exe -ArgumentList $fullArgs -WorkingDirectory $wd -PassThru -WindowStyle Normal
      $obj = Wait-ResultFile $resultPath $TimeoutSec
    } else {
      if (-not (Test-Path $App.Exe)) { throw "Missing exe: $($App.Exe)" }
      $null = Start-Process -FilePath $App.Exe -ArgumentList $argList `
        -WorkingDirectory (Split-Path $App.Exe -Parent) -PassThru -WindowStyle Normal
      $obj = Wait-ResultFile $resultPath $TimeoutSec
    }

    Stop-AppProcesses $App.Processes $App.KillMatch

    if ($null -eq $obj) {
      Log ("{0} TIMEOUT/FAIL run {1}" -f $App.Name, $i)
      throw "ui-bench timeout"
    }

    Log ("{0} startup={1}s render1000={2}s fps={3} filter={4}ms" -f `
      $App.Name, $obj.startup_s, $obj.render_1000_s, $obj.scroll_fps, $obj.filter_response_ms)

    $startup += [double]$obj.startup_s
    $render += [double]$obj.render_1000_s
    $fps += [double]$obj.scroll_fps
    $filter += [double]$obj.filter_response_ms
    Start-Sleep -Milliseconds 800
  }

  function Avg($arr) {
    return [math]::Round((($arr | Measure-Object -Average).Average), 2)
  }

  return [ordered]@{
    name               = $App.Name
    skipped            = $false
    startup_s          = Avg $startup
    render_1000_s      = Avg $render
    scroll_fps         = Avg $fps
    filter_response_ms = Avg $filter
    startup_runs       = $startup
    render_runs        = $render
    fps_runs           = $fps
    filter_runs        = $filter
  }
}

$apps = @(
  @{
    Name = "Avalonia"
    Exe = Join-Path $Root "ToDoApp.Avalonia\bin\Release\net9.0\win-x64\ToDoApp.Avalonia.exe"
    Processes = @("ToDoApp.Avalonia")
  }
  @{
    Name = "Compose"
    Exe = Join-Path $Root "ToDoApp.KotlinMultiplatform\composeApp\build\compose\binaries\main-release\app\com.example.todoappkotlinmultiplatform\com.example.todoappkotlinmultiplatform.exe"
    Processes = @("com.example.todoappkotlinmultiplatform")
  }
  @{
    Name = "Electron"
    Exe = Join-Path $Root "ToDoApp.Electron\release-measure\win-unpacked\Todo App.exe"
    Processes = @("Todo App")
  }
  @{
    Name = "Flutter"
    Exe = Join-Path $Root "todoapp_flutter\build\windows\x64\runner\Release\todoapp_flutter.exe"
    Processes = @("todoapp_flutter")
  }
  @{
    Name = "Tauri"
    Exe = Join-Path $Root "ToDoApp.Tauri\src-tauri\target\release\todoapp-tauri.exe"
    Processes = @("todoapp-tauri")
  }
  @{
    Name = "WPF"
    Exe = Join-Path $Root "ToDoApp.Wpf\bin\Release\net8.0-windows\win-x64\ToDoApp.Wpf.exe"
    Processes = @("ToDoApp.Wpf")
  }
  @{
    Name = "WinUI3"
    Exe = Join-Path $Root "ToDoApp.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\ToDoApp.WinUI.exe"
    Processes = @("ToDoApp.WinUI")
  }
  @{
    Name = "wxWidgets"
    Exe = "uv"
    ExeArgsPrefix = @("run", "python", "main.py")
    WorkingDirectory = Join-Path $Root "ToDoApp.wxWidgets"
    Processes = @("python", "pythonw")
    KillMatch = "ToDoApp\.wxWidgets|todoapp\.wxwidgets|main\.py"
  }
)

Log "ROOT=$Root data=$Data1000"
$results = @()

foreach ($app in $apps) {
  $missing = $false
  if ($app.Name -eq "WinUI3") {
    $manifest = Join-Path $Root "ToDoApp.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\AppxManifest.xml"
    if (-not (Test-Path $manifest)) { $missing = $true; $missPath = $manifest }
  } elseif ($app.ExeArgsPrefix) {
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
    $row = Measure-AppUi $app
    $results += [pscustomobject]$row
    Log ("SUMMARY {0}: startup={1}s render1000={2}s fps={3} filter={4}ms" -f `
      $row.name, $row.startup_s, $row.render_1000_s, $row.scroll_fps, $row.filter_response_ms)
  } catch {
    Log ("FAIL {0}: {1}" -f $app.Name, $_)
    $results += [pscustomobject]@{ name = $app.Name; skipped = $true; reason = "$_" }
  }

  if ($app.Name -eq "WinUI3") {
    $reg = Join-Path $PSScriptRoot "register_winui.ps1"
    try { & $reg -Unregister } catch {}
  }
}

$results | ConvertTo-Json -Depth 6 | Set-Content $JsonFile -Encoding utf8
Log "DONE wrote $JsonFile"

$cleanup = Join-Path $PSScriptRoot "cleanup_local_leftovers.ps1"
if (Test-Path $cleanup) {
  Log "Running cleanup_local_leftovers.ps1"
  & $cleanup
}
