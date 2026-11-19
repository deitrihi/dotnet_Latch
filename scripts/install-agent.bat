@echo off
REM Double-click installer: elevates to admin, bypasses PowerShell execution policy, runs install-agent.ps1.
setlocal

REM Relaunch as administrator if not elevated
net session >nul 2>&1
if errorlevel 1 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

cd /d "%~dp0"
set "SERVER=https://latch.deitrihi.dev"
set "INPUT="
set /p "INPUT=Server URL [%SERVER%]: "
if not "%INPUT%"=="" set "SERVER=%INPUT%"

set "CODE="
set /p "CODE=Pairing code from the web page (leave empty to update only): "

REM Remove "downloaded from another computer" mark so PowerShell does not block the files
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%~dp0' -Recurse | Unblock-File"

if "%CODE%"=="" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-agent.ps1" -ServerUrl "%SERVER%"
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-agent.ps1" -ServerUrl "%SERVER%" -Code "%CODE%"
)

echo.
echo Press any key to close this window...
pause >nul
