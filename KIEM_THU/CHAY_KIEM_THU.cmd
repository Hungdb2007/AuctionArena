@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CHAY_KIEM_THU.ps1"
set "RESULT=%ERRORLEVEL%"
pause
exit /b %RESULT%
