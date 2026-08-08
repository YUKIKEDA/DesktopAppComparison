# Unified clean-build measurement: 3 runs each, average.
# Preconditions: global package caches OK (NuGet/npm/cargo/pub/gradle downloads).
# Project outputs fully cleaned each run. Gradle build cache off. Nuitka ccache off.

$ErrorActionPreference = "Continue"
$Root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { "D:\home\Programs\DesktopAppComparison" }
if (-not (Test-Path (Join-Path $Root "README.md"))) {
  $Root = "D:\home\Programs\DesktopAppComparison"
}
$OutDir = Join-Path $Root "_tools"
$LogFile = Join-Path $OutDir "impl_metrics_log.txt"
$JsonFile = Join-Path $OutDir "impl_metrics_results.json"
$JdkHome = Join-Path $OutDir "jdk-21.0.12+8"
$Runs = 3
$script:LastBuildCode = 0

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
"" | Set-Content $LogFile -Encoding utf8

function Log([string]$msg) {
  $line = "[{0}] {1}" -f (Get-Date -Format "HH:mm:ss"), $msg
  Add-Content $LogFile $line -Encoding utf8
  Write-Host $line
}

function DirMB([string]$p) {
  if (-not (Test-Path $p)) { return $null }
  $sum = (Get-ChildItem -LiteralPath $p -Recurse -File -ErrorAction SilentlyContinue |
    Measure-Object -Property Length -Sum).Sum
  if ($null -eq $sum) { return 0 }
  return [math]::Round($sum / 1MB, 1)
}

function FileMB([string]$p) {
  if (-not (Test-Path $p)) { return $null }
  return [math]::Round((Get-Item -LiteralPath $p).Length / 1MB, 1)
}

function Remove-Path([string]$p) {
  if (Test-Path $p) {
    Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue
  }
}

function Ensure-Jdk {
  $jpackage = Join-Path $JdkHome "bin\jpackage.exe"
  if (Test-Path $jpackage) {
    Log "JDK already present: $JdkHome"
    return
  }
  Log "Downloading portable JDK 21..."
  $zip = Join-Path $OutDir "jdk21.zip"
  Invoke-WebRequest -Uri "https://aka.ms/download-jdk/microsoft-jdk-21.0.12-windows-x64.zip" -OutFile $zip -UseBasicParsing
  Expand-Archive -Path $zip -DestinationPath $OutDir -Force
  Remove-Item $zip -Force -ErrorAction SilentlyContinue
  if (-not (Test-Path $jpackage)) {
    throw "jpackage not found after JDK extract"
  }
  Log "JDK ready"
}

function Invoke-Timed([string]$workdir, [scriptblock]$action) {
  Push-Location $workdir
  try {
    $script:LastBuildCode = 0
    $sw = [Diagnostics.Stopwatch]::StartNew()
    & $action
    $sw.Stop()
    return @{
      Ok   = ($script:LastBuildCode -eq 0)
      Code = $script:LastBuildCode
      Sec  = [math]::Round($sw.Elapsed.TotalSeconds, 1)
    }
  } finally {
    Pop-Location
  }
}

function Measure-App {
  param(
    [string]$Name,
    [scriptblock]$Clean,
    [scriptblock]$Build,
    [scriptblock]$Size,
    [string]$WorkDir
  )
  Log "===== $Name ($Runs clean runs) ====="
  $times = @()
  $sizes = @()
  for ($i = 1; $i -le $Runs; $i++) {
    Log ("{0} run {1}/{2}: clean" -f $Name, $i, $Runs)
    & $Clean
    Log ("{0} run {1}/{2}: build" -f $Name, $i, $Runs)
    $r = Invoke-Timed $WorkDir $Build
    if (-not $r.Ok) {
      Log "$Name run $i FAILED exit=$($r.Code) time=$($r.Sec)s"
      throw "$Name build failed on run $i (exit $($r.Code))"
    }
    $sz = & $Size
    Log "$Name run $i OK time=$($r.Sec)s size=$sz MB"
    $times += $r.Sec
    $sizes += $sz
  }
  $avgT = [math]::Round((($times | Measure-Object -Average).Average), 1)
  $avgS = [math]::Round((($sizes | Where-Object { $null -ne $_ } | Measure-Object -Average).Average), 1)
  Log "$Name AVG time=$avgT s size=$avgS MB (times=$($times -join ',') sizes=$($sizes -join ','))"
  return [pscustomobject]@{
    name         = $Name
    times        = $times
    sizes        = $sizes
    avg_time_s   = $avgT
    avg_size_mb  = $avgS
  }
}

function Clean-DotNet([string]$projDir) {
  Push-Location $projDir
  try {
    cmd /c "dotnet clean -c Release --nologo -v q" | Out-Null
  } finally { Pop-Location }
  Remove-Path (Join-Path $projDir "bin")
  Remove-Path (Join-Path $projDir "obj")
}

$AvaloniaDir = Join-Path $Root "ToDoApp.Avalonia"
$WpfDir = Join-Path $Root "ToDoApp.Wpf"
$WinUiDir = Join-Path $Root "ToDoApp.WinUI"
$ElectronDir = Join-Path $Root "ToDoApp.Electron"
$FlutterDir = Join-Path $Root "todoapp_flutter"
$TauriDir = Join-Path $Root "ToDoApp.Tauri"
$TauriRust = Join-Path $TauriDir "src-tauri"
$ComposeDir = Join-Path $Root "ToDoApp.KotlinMultiplatform"
$WxDir = Join-Path $Root "ToDoApp.wxWidgets"
$GradleWrapperJar = Join-Path $ComposeDir "gradle\wrapper\gradle-wrapper.jar"
$JavaExe = Join-Path $JdkHome "bin\java.exe"
$WxPython = Join-Path $WxDir ".venv\Scripts\python.exe"

Log "ROOT=$Root"
Log "Ensuring package restores (not timed)..."

Push-Location $AvaloniaDir; cmd /c "dotnet restore --nologo -v q"; Pop-Location
Push-Location $WpfDir; cmd /c "dotnet restore --nologo -v q"; Pop-Location
Push-Location $WinUiDir; cmd /c "dotnet restore -p:Platform=x64 --nologo -v q"; Pop-Location
Push-Location $ElectronDir; cmd /c "npm install --silent"; Pop-Location
Push-Location $TauriDir; cmd /c "npm install --silent"; Pop-Location
Push-Location $FlutterDir; cmd /c "flutter pub get"; Pop-Location
Ensure-Jdk

# Warm Gradle wrapper download (untimed)
Push-Location $ComposeDir
try {
  $env:JAVA_HOME = $JdkHome
  & $JavaExe "-Dorg.gradle.appname=gradlew" -classpath $GradleWrapperJar org.gradle.wrapper.GradleWrapperMain "--version" | Out-Null
} finally { Pop-Location }

$results = @()

$results += Measure-App -Name "Avalonia" -WorkDir $AvaloniaDir `
  -Clean { Clean-DotNet $AvaloniaDir } `
  -Build {
    cmd /c "dotnet publish -c Release -p:PublishProfile=FolderProfile --nologo -v q"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { DirMB (Join-Path $AvaloniaDir "bin\Release\net9.0\publish\win-x64") }

$results += Measure-App -Name "WPF" -WorkDir $WpfDir `
  -Clean { Clean-DotNet $WpfDir } `
  -Build {
    cmd /c "dotnet publish -c Release -p:PublishProfile=FolderProfile --nologo -v q"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { DirMB (Join-Path $WpfDir "bin\Release\net8.0-windows\publish\win-x64") }

$results += Measure-App -Name "WinUI3" -WorkDir $WinUiDir `
  -Clean {
    Clean-DotNet $WinUiDir
    Remove-Path (Join-Path $WinUiDir "bin")
    Remove-Path (Join-Path $WinUiDir "obj")
  } `
  -Build {
    cmd /c "dotnet publish -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:SelfContained=true --nologo -v q"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { DirMB (Join-Path $WinUiDir "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish") }

$results += Measure-App -Name "Electron" -WorkDir $ElectronDir `
  -Clean {
    Remove-Path (Join-Path $ElectronDir "dist")
    Remove-Path (Join-Path $ElectronDir "dist-electron")
    Remove-Path (Join-Path $ElectronDir "release-measure")
    Remove-Path (Join-Path $ElectronDir "release")
  } `
  -Build {
    cmd /c "npx tsc && npx vite build && npx electron-builder"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { DirMB (Join-Path $ElectronDir "release-measure\win-unpacked") }

$results += Measure-App -Name "Flutter" -WorkDir $FlutterDir `
  -Clean {
    Push-Location $FlutterDir
    try { cmd /c "flutter clean" } finally { Pop-Location }
    Remove-Path (Join-Path $FlutterDir "build")
  } `
  -Build {
    cmd /c "flutter build windows --release"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { DirMB (Join-Path $FlutterDir "build\windows\x64\runner\Release") }

$results += Measure-App -Name "Tauri" -WorkDir $TauriDir `
  -Clean {
    Remove-Path (Join-Path $TauriDir "dist")
    Push-Location $TauriRust
    try { cmd /c "cargo clean" } finally { Pop-Location }
  } `
  -Build {
    cmd /c "npm run build"
    if ($LASTEXITCODE -ne 0) { $script:LastBuildCode = $LASTEXITCODE; return }
    Push-Location $TauriRust
    try {
      cmd /c "cargo build --release"
      $script:LastBuildCode = $LASTEXITCODE
    } finally { Pop-Location }
  } `
  -Size { FileMB (Join-Path $TauriRust "target\release\todoapp-tauri.exe") }

$results += Measure-App -Name "Compose" -WorkDir $ComposeDir `
  -Clean {
    Push-Location $ComposeDir
    try {
      $env:JAVA_HOME = $JdkHome
      & $JavaExe "-Dorg.gradle.appname=gradlew" -classpath $GradleWrapperJar org.gradle.wrapper.GradleWrapperMain `
        ":composeApp:clean" "--no-build-cache" "--no-configuration-cache" "-q"
    } finally { Pop-Location }
    Remove-Path (Join-Path $ComposeDir "composeApp\build")
    Remove-Path (Join-Path $ComposeDir "build")
  } `
  -Build {
    $env:JAVA_HOME = $JdkHome
    & $JavaExe "-Dorg.gradle.appname=gradlew" -classpath $GradleWrapperJar org.gradle.wrapper.GradleWrapperMain `
      ":composeApp:packageReleaseDistributionForCurrentOS" "--no-build-cache" "--no-configuration-cache" "-q"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size {
    FileMB (Join-Path $ComposeDir "composeApp\build\compose\binaries\main-release\msi\com.example.todoappkotlinmultiplatform-1.0.0.msi")
  }

$results += Measure-App -Name "wxWidgets" -WorkDir $WxDir `
  -Clean {
    Remove-Path (Join-Path $WxDir "dist\main.build")
    Remove-Path (Join-Path $WxDir "dist\main.dist")
    Remove-Path (Join-Path $WxDir "dist\main.onefile-build")
    Remove-Path (Join-Path $WxDir "dist\TodoApp.exe")
  } `
  -Build {
    cmd /c "`"$WxPython`" -m nuitka --standalone --onefile --disable-ccache --include-package=wx --include-package-data=wx --windows-console-mode=disable --include-module=models --include-module=views --include-module=controllers --include-module=utils --output-dir=dist --output-filename=TodoApp.exe main.py"
    $script:LastBuildCode = $LASTEXITCODE
  } `
  -Size { FileMB (Join-Path $WxDir "dist\TodoApp.exe") }

$results | ConvertTo-Json -Depth 5 | Set-Content $JsonFile -Encoding utf8
Log "DONE wrote $JsonFile"
$results | ForEach-Object {
  Log ("SUMMARY {0}: avg_time={1}s avg_size={2}MB" -f $_.name, $_.avg_time_s, $_.avg_size_mb)
}
