# Register / unregister the WinUI3 Todo app (development Appx identity).
# Direct .exe launch crashes with 0xC000027B; packaged activation is required to run.
#
# Usage:
#   .\scripts\register_winui.ps1              # register only
#   .\scripts\register_winui.ps1 -Launch      # register (if needed) and launch
#   .\scripts\register_winui.ps1 -Unregister  # remove Start menu / Appx registration

param(
  [switch]$Launch,
  [switch]$Unregister
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$Manifest = Join-Path $Root "ToDoApp.WinUI\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\AppxManifest.xml"
$PFN = "d58fe1bb-f479-4f19-b358-06dee335f74c_k6bmzwkfnste6"
$AppId = "$PFN!App"

if ($Unregister) {
  $pkg = Get-AppxPackage | Where-Object PackageFamilyName -eq $PFN
  if ($pkg) {
    Remove-AppxPackage -Package $pkg.PackageFullName
    Write-Host "Unregistered $($pkg.PackageFullName)"
    Write-Host "Note: build output under bin\ was not deleted."
  } else {
    Write-Host "Not registered: $PFN"
  }
  return
}

if (-not (Test-Path $Manifest)) {
  throw "Missing AppxManifest.xml. Build first: dotnet build ToDoApp.WinUI\ToDoApp.WinUI.csproj -c Release -p:Platform=x64"
}

if (-not (Get-AppxPackage | Where-Object PackageFamilyName -eq $PFN)) {
  Add-AppxPackage -Register $Manifest -ForceApplicationShutdown
  Write-Host "Registered $PFN"
  Write-Host "InstallLocation -> $(Split-Path $Manifest -Parent)"
} else {
  Write-Host "Already registered: $PFN"
}

if ($Launch) {
  Start-Process explorer.exe -ArgumentList "shell:AppsFolder\$AppId"
  Write-Host "Launched shell:AppsFolder\$AppId"
}
