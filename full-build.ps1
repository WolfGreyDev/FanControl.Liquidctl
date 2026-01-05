# Consolidated Build Script for FanControl.Liquidctl
# Requirements: Python 3.11+, PyInstaller, colorlog, hidapi, pyusb, docopt

Write-Host "--- Stage 1: Building liquidctl.exe ---" -ForegroundColor Cyan
if (!(Test-Path "liquidctl")) {
    Write-Host "Cloning liquidctl fork..."
    git clone https://github.com/SuspiciousActivity/liquidctl.git
}

Push-Location liquidctl

Write-Host "Setting up local virtual environment (.venv)..."
if (!(Test-Path ".venv")) {
    py -m venv .venv
}

Write-Host "Activating virtual environment..."
& ".\.venv\Scripts\Activate.ps1"

Write-Host "Installing build requirements locally..."
pip install pyinstaller colorlog crcmod pillow docopt hidapi pyusb libusb-package winusbcdc

Write-Host "Running PyInstaller..."
pyinstaller -F liquidctl/cli.py --name liquidctl

if (Test-Path "dist/liquidctl.exe") {
    Copy-Item "dist/liquidctl.exe" "../liquidctl.exe" -Force
    Write-Host "Successfully built and copied liquidctl.exe" -ForegroundColor Green
} else {
    Write-Error "Failed to build liquidctl.exe"
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "`n--- Stage 2: Building FanControl.Liquidctl (Release) ---" -ForegroundColor Cyan
dotnet build -c Release

Write-Host "`n--- Stage 3: Creating Release Package ---" -ForegroundColor Cyan
if (Test-Path ".\build-release.ps1") {
    .\build-release.ps1
    Write-Host "Release created: FanControl.Liquidctl.zip" -ForegroundColor Green
} else {
    Write-Error "build-release.ps1 not found"
}
