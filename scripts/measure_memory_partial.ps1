# Resume memory measurement for WinUI3 / wxWidgets / Tauri (with WebView2).
# Peak = max(private MB across empty/10/100/1000 averages).

$ErrorActionPreference = "Continue"
$Root = "D:\home\Programs\DesktopAppComparison"
$DataDir = Join-Path $Root "data"
$OutDir = Join-Path $Root "_tools"
$LogFile = Join-Path $OutDir "memory_metrics_log2.txt"
$JsonFile = Join-Path $OutDir "memory_metrics_results_partial.json"
$Runs = 3

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
"" | Set-Content $LogFile -Encoding utf8

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $msg
  Add-Content $LogFile $line -Encoding utf8
  Write-Host $line
}

function To-MB([long]$bytes) { [math]::Round($bytes / 1MB, 1) }

function Stop-Names([string[]]$names) {
  foreach ($n in $names) {
    Get-Process -Name $n -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  }
  Start-Sleep -Milliseconds 800
}

function Get-RelatedProcesses([string[]]$names, [int]$rootPid, [bool]$includeWebView) {
  $procs = @()
  foreach ($n in $names) {
    $procs += @(Get-Process -Name $n -ErrorAction SilentlyContinue)
  }
  if ($includeWebView) {
    $wv = @(Get-Process -Name "msedgewebview2" -ErrorAction SilentlyContinue)
    if ($wv.Count -gt 0 -and $rootPid -gt 0) {
      # Include WebView2 processes started around this app (same session tree via CIM parent)
      $all = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'msedgewebview2|todoapp-tauri' }
      $ids = New-Object System.Collections.Generic.HashSet[int]
      [void]$ids.Add($rootPid)
      $changed = $true
      while ($changed) {
        $changed = $false
        foreach ($c in $all) {
          if ($ids.Contains([int]$c.ParentProcessId) -and -not $ids.Contains([int]$c.ProcessId)) {
            [void]$ids.Add([int]$c.ProcessId)
            $changed = $true
          }
        }
      }
      foreach ($id in $ids) {
        $p = Get-Process -Id $id -ErrorAction SilentlyContinue
        if ($p) { $procs += $p }
      }
    }
  }
  return @($procs | Sort-Object Id -Unique)
}

function Measure-PrivateMB([string[]]$names, [int]$rootPid, [bool]$includeWebView) {
  $procs = Get-RelatedProcesses $names $rootPid $includeWebView
  if ($procs.Count -eq 0) { return $null }
  $priv = ($procs | Measure-Object PrivateMemorySize64 -Sum).Sum
  return @{
    PrivateMB = To-MB ([long]$priv)
    Count     = $procs.Count
  }
}

function Measure-AppStates {
  param($App)

  $row = [ordered]@{ name = $App.Name }
  $states = @(
    @{ Key = "empty"; File = Join-Path $DataDir "project_0.json" }
    @{ Key = "n10"; File = Join-Path $DataDir "project_10.json" }
    @{ Key = "n100"; File = Join-Path $DataDir "project_100.json" }
    @{ Key = "n1000"; File = Join-Path $DataDir "project_1000.json" }
  )

  foreach ($st in $states) {
    $vals = @()
    for ($i = 1; $i -le $Runs; $i++) {
      Stop-Names $App.KillNames
      Log ("{0} {1} run {2}/{3}" -f $App.Name, $st.Key, $i, $Runs)
      $proc = Start-Process -FilePath $App.Exe -ArgumentList @($st.File) -PassThru -WorkingDirectory (Split-Path $App.Exe)
      Start-Sleep -Milliseconds $App.SettleMs
      if ($proc.HasExited) {
        Log ("{0} exited early code={1}" -f $App.Name, $proc.ExitCode)
        throw "$($App.Name) exited early"
      }
      $m = Measure-PrivateMB $App.ProcessNames $proc.Id $App.IncludeWebView
      if ($null -eq $m) { throw "$($App.Name) process missing" }
      Log ("{0} private={1} procs={2}" -f $App.Name, $m.PrivateMB, $m.Count)
      $vals += $m.PrivateMB
      # Stop measured process tree (includes WebView2 children when applicable)
      $tree = Get-RelatedProcesses $App.ProcessNames $proc.Id $App.IncludeWebView
      foreach ($tp in $tree) {
        Stop-Process -Id $tp.Id -Force -ErrorAction SilentlyContinue
      }
      Stop-Names $App.KillNames
      Start-Sleep -Milliseconds 800
    }
    $row[$st.Key] = [math]::Round((($vals | Measure-Object -Average).Average), 1)
    $row[($st.Key + "_runs")] = $vals
  }

  $row["peak"] = [math]::Max($row.empty, [math]::Max($row.n10, [math]::Max($row.n100, $row.n1000)))
  Log ("SUMMARY {0}: empty={1} 10={2} 100={3} 1000={4} peak={5}" -f $row.name, $row.empty, $row.n10, $row.n100, $row.n1000, $row.peak)
  return [pscustomobject]$row
}

$apps = @(
  @{
    Name = "Tauri"
    Exe = Join-Path $Root "ToDoApp.Tauri\src-tauri\target\release\todoapp-tauri.exe"
    ProcessNames = @("todoapp-tauri")
    KillNames = @("todoapp-tauri", "msedgewebview2")
    IncludeWebView = $true
    SettleMs = 10000
  }
  @{
    Name = "WinUI3"
    Exe = Join-Path $Root "ToDoApp.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\ToDoApp.WinUI.exe"
    ProcessNames = @("ToDoApp.WinUI")
    KillNames = @("ToDoApp.WinUI")
    IncludeWebView = $false
    SettleMs = 8000
  }
  @{
    Name = "wxWidgets"
    Exe = Join-Path $Root "ToDoApp.wxWidgets\dist\TodoApp.exe"
    ProcessNames = @("TodoApp")
    KillNames = @("TodoApp")
    IncludeWebView = $false
    SettleMs = 12000
  }
)

# Don't kill ALL msedgewebview2 globally in Stop before measure - only after. For Tauri kill names include it after.
# Before start, only kill todoapp-tauri to avoid killing other apps' webviews if any.
$apps[0].KillNames = @("todoapp-tauri")

$results = @()
foreach ($a in $apps) {
  Log ("===== {0} =====" -f $a.Name)
  $results += Measure-AppStates $a
  if ($a.Name -eq "Tauri") {
    # cleanup leftover webviews from this app best-effort via process tree already stopped with todoapp
  }
}

$results | ConvertTo-Json -Depth 5 | Set-Content $JsonFile -Encoding utf8
Log "DONE wrote $JsonFile"

$cleanup = Join-Path $PSScriptRoot "cleanup_local_leftovers.ps1"
if (Test-Path $cleanup) {
  Log "Running cleanup_local_leftovers.ps1"
  & $cleanup
}
