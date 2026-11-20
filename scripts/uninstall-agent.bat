@echo off
REM Double-click uninstaller: elevates to admin, confirms, runs uninstall-agent.ps1 (firewall/hosts cleanup + removal).

REM Relaunch as administrator if not elevated
net session >nul 2>&1
if errorlevel 1 (
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)

echo This removes Latch from this PC: stops the service, clears firewall/site blocks, deletes all files.
set "ANSWER="
set /p "ANSWER=Continue? (Y/N): "
if /i not "%ANSWER%"=="Y" (
  echo Cancelled.
  pause
  exit /b
)

REM Run a temp copy, and keep the rest in one block: this .bat may live in the folder being deleted,
REM and cmd reads a .bat line by line, so it must not need to read this file again.
copy /y "%~dp0uninstall-agent.ps1" "%TEMP%\latch-uninstall.ps1" >nul
(
  cd /d "%TEMP%"
  powershell -NoProfile -ExecutionPolicy Bypass -File "%TEMP%\latch-uninstall.ps1"
  del "%TEMP%\latch-uninstall.ps1" >nul 2>&1
  echo.
  echo Press any key to close this window...
  pause >nul
  REM exit (not exit /b): after the folder is gone, returning to cmd prints "path not found"
  exit
)
