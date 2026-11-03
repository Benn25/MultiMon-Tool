# Builds the release: a single self-contained DualMonitorTools-Setup.exe in .\dist
# (no .NET installation is needed on the PC it's run on).
#
# Run from the repository folder:   powershell -ExecutionPolicy Bypass -File .\build-release.ps1
#
# The setup file is DMT itself: run under a name containing "Setup" it installs itself,
# renamed to DMT.exe it runs as a portable copy.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$publish = Join-Path $root 'dist\publish'
$dist = Join-Path $root 'dist'

Write-Host 'Running unit tests...'
dotnet test (Join-Path $root 'DMT\DMT.sln') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed' }

Write-Host 'Publishing...'
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $root 'DMT\DMT\DMT.csproj') -c Release -r win-x64 --self-contained true `
	-p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
	-p:DebugType=none -o $publish --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }

$setup = Join-Path $dist 'DualMonitorTools-Setup.exe'
Copy-Item (Join-Path $publish 'DMT.exe') $setup -Force
Copy-Item (Join-Path $root 'COPYING.txt') (Join-Path $dist 'COPYING.txt') -Force

$size = [math]::Round((Get-Item $setup).Length / 1MB, 1)
$version = (Get-Item $setup).VersionInfo.ProductVersion
Write-Host "Built $setup ($size MB, version $version)"
