@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0TAO_SETUP.ps1"
set "RESULT=%ERRORLEVEL%"
pause
exit /b %RESULT%
