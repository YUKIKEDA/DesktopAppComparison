# Remove Start-menu / Appx / app-data / build leftovers from local comparison runs.
# Does NOT touch Windows system apps (e.g. ClickToDo / CoreAI).

$ErrorActionPreference = "Continue"
$Root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { "D:\home\Programs\DesktopAppComparison" }

function Remove-IfExists([string]$path) {
  if (Test-Path -LiteralPath $path) {
    Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $path) {
      Write-Host "FAIL: $path"
    } else {
      Write-Host "Removed: $path"
    }
  }
}

Write-Host "=== Stop comparison processes (best-effort) ==="
@(
  "Todo App", "todoapp-electron", "todoapp_flutter", "ToDoApp.WinUI", "ToDoApp.Avalonia",
  "ToDoApp.Wpf", "TodoApp.wxWidgets", "todoapp-tauri", "TodoApp"
) | ForEach-Object {
  Get-Process -Name $_ -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

Write-Host "=== Unregister WinUI Appx (if any) ==="
$pkg = Get-AppxPackage | Where-Object { $_.PackageFullName -match 'd58fe1bb' -or $_.InstallLocation -match 'ToDoApp\.WinUI' }
foreach ($p in @($pkg)) {
  Remove-AppxPackage -Package $p.PackageFullName -ErrorAction SilentlyContinue
  Write-Host "Unregistered $($p.PackageFullName)"
}

Write-Host "=== Start Menu / Desktop shortcuts ==="
@(
  "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Todo App.lnk",
  "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\todoapp_flutter.lnk",
  "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\ToDoApp.WinUI.lnk",
  "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Todo App.lnk",
  "$env:USERPROFILE\Desktop\Todo App.lnk",
  "$env:PUBLIC\Desktop\Todo App.lnk"
) | ForEach-Object { Remove-IfExists $_ }

# Any leftover .lnk under Start Menu mentioning our product names
Get-ChildItem @(
  "$env:APPDATA\Microsoft\Windows\Start Menu\Programs",
  "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
) -Filter "*.lnk" -Recurse -ErrorAction SilentlyContinue |
  Where-Object { $_.BaseName -match '^(Todo App|todoapp_flutter|ToDoApp\.|TodoApp)' } |
  ForEach-Object { Remove-IfExists $_.FullName }

Write-Host "=== App data (persist dirs from runs) ==="
@(
  "$env:APPDATA\com.yuuuu.todoapp-tauri",
  "$env:APPDATA\Todo App",
  "$env:APPDATA\todoapp-electron",
  "$env:APPDATA\ToDoApp.Avalonia",
  "$env:APPDATA\ToDoApp.Wpf",
  "$env:APPDATA\TodoApp.wxWidgets",
  "$env:LOCALAPPDATA\com.yuuuu.todoapp-tauri",
  "$env:LOCALAPPDATA\ToDoApp.WinUI",
  "$env:LOCALAPPDATA\ToDoApp.Wpf",
  "$env:LOCALAPPDATA\ToDoApp.WinForms",
  "$env:USERPROFILE\Documents\todoapp_flutter",
  "$env:USERPROFILE\.todoapp.kotlinmultiplatform"
) | ForEach-Object { Remove-IfExists $_ }

Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -match 'd58fe1bb' } |
  ForEach-Object { Remove-IfExists $_.FullName }

Write-Host "=== Crash dumps ==="
Get-ChildItem "$env:LOCALAPPDATA\CrashDumps" -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -match '^(ToDoApp|TodoApp|todoapp)' } |
  ForEach-Object { Remove-IfExists $_.FullName }

Write-Host "=== Repo build leftovers ==="
@(
  (Join-Path $Root "ToDoApp.Electron\release"),
  (Join-Path $Root "ToDoApp.Electron\release-measure"),
  (Join-Path $Root "ToDoApp.WinUI\AppPackages"),
  (Join-Path $Root "ToDoApp.WinUI\release-measure")
) | ForEach-Object { Remove-IfExists $_ }

Write-Host "=== Done ==="
$left = Get-StartApps | Where-Object {
  $_.Name -match 'Todo|ToDo|todoapp|Flutter' -and $_.AppID -notmatch 'CoreAI|ClickToDo'
}
if ($left) {
  $left | ForEach-Object { Write-Host "Still listed in Start: $($_.Name) ($($_.AppID))" }
} else {
  Write-Host "StartApps: no comparison Todo leftovers (ClickToDo ignored)."
}
