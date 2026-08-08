# Rebuild all comparison apps for CPU bench measurement (Release artifacts).
$ErrorActionPreference = "Continue"
$Root = Split-Path $PSScriptRoot -Parent
$Log = Join-Path $Root "_tools\cpu_build_log.txt"
New-Item -ItemType Directory -Force -Path (Join-Path $Root "_tools") | Out-Null
"" | Set-Content $Log -Encoding utf8

function Log($m) {
  $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $m
  Add-Content $Log $line; Write-Host $line
}

function Run($name, $cmd) {
  Log "BUILD $name"
  Push-Location $Root
  try {
    cmd /c $cmd
    if ($LASTEXITCODE -ne 0) { Log "FAIL $name exit=$LASTEXITCODE"; return $false }
    Log "OK $name"
    return $true
  } finally { Pop-Location }
}

$ok = $true
$ok = (Run "Avalonia" "dotnet publish ToDoApp.Avalonia\ToDoApp.Avalonia.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o ToDoApp.Avalonia\bin\Release\net9.0\publish\win-x64") -and $ok
$ok = (Run "WPF" "dotnet publish ToDoApp.Wpf\ToDoApp.Wpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ToDoApp.Wpf\bin\Release\net8.0-windows\publish\win-x64") -and $ok
$ok = (Run "WinUI" "dotnet build ToDoApp.WinUI\ToDoApp.WinUI.csproj -c Release -p:Platform=x64 && dotnet publish ToDoApp.WinUI\ToDoApp.WinUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=true -o ToDoApp.WinUI\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish") -and $ok

# Electron
Log "BUILD Electron"
Push-Location (Join-Path $Root "ToDoApp.Electron")
try {
  npm run build
  if ($LASTEXITCODE -ne 0) { $ok = $false; Log "FAIL Electron" } else { Log "OK Electron" }
} finally { Pop-Location }

# Flutter
Log "BUILD Flutter"
Push-Location (Join-Path $Root "todoapp_flutter")
try {
  flutter build windows --release
  if ($LASTEXITCODE -ne 0) { $ok = $false; Log "FAIL Flutter" } else { Log "OK Flutter" }
} finally { Pop-Location }

# Tauri
Log "BUILD Tauri"
Push-Location (Join-Path $Root "ToDoApp.Tauri")
try {
  npm run tauri build
  if ($LASTEXITCODE -ne 0) {
    # fallback: vite + cargo
    npx vite build
    Push-Location "src-tauri"
    cargo build --release
    Pop-Location
  }
  if ($LASTEXITCODE -ne 0) { $ok = $false; Log "FAIL Tauri" } else { Log "OK Tauri" }
} finally { Pop-Location }

# Compose
Log "BUILD Compose"
Push-Location (Join-Path $Root "ToDoApp.KotlinMultiplatform")
try {
  if (Test-Path ".\gradlew.bat") {
    .\gradlew.bat :composeApp:packageReleaseDistributionForCurrentOS --no-build-cache --no-configuration-cache
  } else {
    Log "FAIL Compose: no gradlew"
    $ok = $false
  }
  if ($LASTEXITCODE -ne 0) { $ok = $false; Log "FAIL Compose" } else { Log "OK Compose" }
} finally { Pop-Location }

# wx - use existing dist if present; optional rebuild is slow (Nuitka)
if (Test-Path (Join-Path $Root "ToDoApp.wxWidgets\dist\TodoApp.exe")) {
  Log "SKIP wxWidgets rebuild (dist exists) — ensure cpu_bench.py is in onefile; prefer venv run if needed"
} else {
  Log "WARN wxWidgets dist missing"
}

if ($ok) { Log "ALL BUILDS OK (wx may need separate)" } else { Log "SOME BUILDS FAILED"; exit 1 }
