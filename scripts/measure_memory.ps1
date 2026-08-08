# Memory measurement: private working set (PrivateMemorySize64) in MB.
# Peak column: PeakWorkingSet64 at the 1000-item state.
# Protocol: clear persist (empty) or launch with data/*.json; settle; average of 3 runs.

$ErrorActionPreference = "Continue"
$Root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { "D:\home\Programs\DesktopAppComparison" }
$DataDir = Join-Path $Root "data"
$OutDir = Join-Path $Root "_tools"
$LogFile = Join-Path $OutDir "memory_metrics_log.txt"
$JsonFile = Join-Path $OutDir "memory_metrics_results.json"
$Runs = 3
$SettleMs = 8000
$SettleMsWx = 12000

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
"" | Set-Content $LogFile -Encoding utf8

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $msg
  Add-Content $LogFile $line -Encoding utf8
  Write-Host $line
}

function To-MB([long]$bytes) {
  return [math]::Round($bytes / 1MB, 1)
}

function Stop-AppProcesses([string[]]$names) {
  foreach ($n in $names) {
    Get-Process -Name $n -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  }
  Start-Sleep -Milliseconds 800
}

function Clear-Persist([string[]]$paths) {
  foreach ($p in $paths) {
    if (Test-Path $p) {
      Remove-Item -LiteralPath $p -Force -ErrorAction SilentlyContinue
    }
    $dir = Split-Path $p -Parent
    if ($dir -and -not (Test-Path $dir)) {
      New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
  }
}

function Measure-PrivateMB([string[]]$processNames) {
  $procs = @()
  foreach ($n in $processNames) {
    $procs += @(Get-Process -Name $n -ErrorAction SilentlyContinue)
  }
  if ($procs.Count -eq 0) { return $null }
  $priv = ($procs | Measure-Object -Property PrivateMemorySize64 -Sum).Sum
  $peak = ($procs | Measure-Object -Property PeakWorkingSet64 -Maximum).Maximum
  return @{
    PrivateMB = To-MB ([long]$priv)
    PeakWsMB  = To-MB ([long]$peak)
    Count     = $procs.Count
  }
}

function Measure-State {
  param(
    [string]$AppName,
    [string]$Exe,
    [string[]]$ProcessNames,
    [string[]]$PersistPaths,
    [string]$DataFile,   # $null = empty
    [int]$Settle
  )

  $timesPriv = @()
  $timesPeak = @()
  for ($i = 1; $i -le $Runs; $i++) {
    Stop-AppProcesses $ProcessNames
    Clear-Persist $PersistPaths

    $args = @($DataFile)

    Log ("{0} state={1} run {2}/{3}" -f $AppName, (Split-Path $DataFile -Leaf), $i, $Runs)

    if (-not (Test-Path $Exe)) {
      throw "Missing exe: $Exe"
    }

    $pInfo = @{
      FilePath         = $Exe
      PassThru         = $true
      WindowStyle      = "Normal"
      WorkingDirectory = (Split-Path $Exe -Parent)
    }
    if ($args.Count -gt 0) { $pInfo.ArgumentList = $args }

    $proc = Start-Process @pInfo
    Start-Sleep -Milliseconds $Settle

    $m = Measure-PrivateMB $ProcessNames
    if ($null -eq $m) {
      # fallback: started process id
      try {
        $rp = Get-Process -Id $proc.Id -ErrorAction Stop
        $m = @{
          PrivateMB = To-MB $rp.PrivateMemorySize64
          PeakWsMB  = To-MB $rp.PeakWorkingSet64
          Count     = 1
        }
      } catch {
        Log ("{0} FAILED: process not found" -f $AppName)
        Stop-AppProcesses $ProcessNames
        throw
      }
    }

    Log ("{0} private={1} MB peakWS={2} MB procs={3}" -f $AppName, $m.PrivateMB, $m.PeakWsMB, $m.Count)
    $timesPriv += $m.PrivateMB
    $timesPeak += $m.PeakWsMB

    Stop-AppProcesses $ProcessNames
    Start-Sleep -Milliseconds 500
  }

  return @{
    private_avg = [math]::Round((($timesPriv | Measure-Object -Average).Average), 1)
    private_runs = $timesPriv
    peakws_avg = [math]::Round((($timesPeak | Measure-Object -Average).Average), 1)
  }
}

$apps = @(
  @{
    Name = "Avalonia"
    Exe = Join-Path $Root "ToDoApp.Avalonia\bin\Release\net9.0\publish\win-x64\ToDoApp.Avalonia.exe"
    Processes = @("ToDoApp.Avalonia")
    Persist = @((Join-Path $env:APPDATA "ToDoApp.Avalonia\data\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "Compose"
    Exe = Join-Path $Root "ToDoApp.KotlinMultiplatform\composeApp\build\compose\binaries\main-release\app\com.example.todoappkotlinmultiplatform\com.example.todoappkotlinmultiplatform.exe"
    Processes = @("com.example.todoappkotlinmultiplatform")
    Persist = @((Join-Path $env:USERPROFILE ".todoapp.kotlinmultiplatform\data\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "Electron"
    Exe = Join-Path $Root "ToDoApp.Electron\release-measure\win-unpacked\Todo App.exe"
    Processes = @("Todo App")
    Persist = @((Join-Path $env:APPDATA "Todo App\data\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "Flutter"
    Exe = Join-Path $Root "todoapp_flutter\build\windows\x64\runner\Release\todoapp_flutter.exe"
    Processes = @("todoapp_flutter")
    Persist = @((Join-Path $env:USERPROFILE "Documents\todoapp_flutter\data\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "Tauri"
    Exe = Join-Path $Root "ToDoApp.Tauri\src-tauri\target\release\todoapp-tauri.exe"
    Processes = @("todoapp-tauri")
    Persist = @((Join-Path $env:APPDATA "com.yuuuu.todoapp-tauri\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "WPF"
    Exe = Join-Path $Root "ToDoApp.Wpf\bin\Release\net8.0-windows\publish\win-x64\ToDoApp.Wpf.exe"
    Processes = @("ToDoApp.Wpf")
    Persist = @((Join-Path $env:APPDATA "ToDoApp.Wpf\data\project.json"))
    Settle = $SettleMs
  }
  @{
    Name = "WinUI3"
    Exe = Join-Path $Root "ToDoApp.WinUI\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\ToDoApp.WinUI.exe"
    Processes = @("ToDoApp.WinUI")
    # unpackaged LocalFolder often under AppData\Local\Packages or exe-relative; also try LocalApplicationData
    Persist = @(
      (Join-Path $env:LOCALAPPDATA "Packages\*\LocalState\Data\project.json"),
      (Join-Path $env:LOCALAPPDATA "ToDoApp.WinUI\Data\project.json")
    )
    Settle = $SettleMs
  }
  @{
    Name = "wxWidgets"
    Exe = Join-Path $Root "ToDoApp.wxWidgets\dist\TodoApp.exe"
    Processes = @("TodoApp")
    Persist = @((Join-Path $env:APPDATA "TodoApp.wxWidgets\data\project.json"))
    Settle = $SettleMsWx
  }
)

# Expand WinUI persist globs
foreach ($a in $apps) {
  if ($a.Name -eq "WinUI3") {
    $expanded = @()
    foreach ($p in $a.Persist) {
      if ($p -match '\*') {
        $expanded += @(Get-Item $p -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
      } else {
        $expanded += $p
      }
    }
    # WinUI unpackaged often uses ApplicationData in LocalState next to package; also check common path
    $expanded += (Join-Path $env:LOCALAPPDATA "ToDoApp.WinUI\LocalState\Data\project.json")
    $a.Persist = $expanded | Select-Object -Unique
  }
}

$states = @(
  @{ Key = "empty"; File = Join-Path $DataDir "project_0.json" }
  @{ Key = "n10"; File = Join-Path $DataDir "project_10.json" }
  @{ Key = "n100"; File = Join-Path $DataDir "project_100.json" }
  @{ Key = "n1000"; File = Join-Path $DataDir "project_1000.json" }
)

Log "ROOT=$Root"
$results = @()

foreach ($app in $apps) {
  if (-not (Test-Path $app.Exe)) {
    Log ("SKIP {0}: missing {1}" -f $app.Name, $app.Exe)
    $results += [pscustomobject]@{
      name = $app.Name
      skipped = $true
      reason = "missing exe"
    }
    continue
  }

  Log ("===== {0} =====" -f $app.Name)
  $row = [ordered]@{ name = $app.Name; skipped = $false }

  foreach ($st in $states) {
    $r = Measure-State -AppName $app.Name -Exe $app.Exe -ProcessNames $app.Processes `
      -PersistPaths $app.Persist -DataFile $st.File -Settle $app.Settle
    $row[$st.Key] = $r.private_avg
    $row[($st.Key + "_runs")] = $r.private_runs
    if ($st.Key -eq "n1000") {
      $row["peak"] = $r.peakws_avg
    }
  }

  # peak already set from n1000 peakws_avg
  $results += [pscustomobject]$row
  Log ("SUMMARY {0}: empty={1} 10={2} 100={3} 1000={4} peakWS={5}" -f `
    $row.name, $row.empty, $row.n10, $row.n100, $row.n1000, $row.peak)
}

$results | ConvertTo-Json -Depth 6 | Set-Content $JsonFile -Encoding utf8
Log "DONE wrote $JsonFile"

$cleanup = Join-Path $PSScriptRoot "cleanup_local_leftovers.ps1"
if (Test-Path $cleanup) {
  Log "Running cleanup_local_leftovers.ps1"
  & $cleanup
}
