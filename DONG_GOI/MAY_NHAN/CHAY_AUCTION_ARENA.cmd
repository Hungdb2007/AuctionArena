@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0KIEM_TRA_VA_CHAY.ps1"
if errorlevel 1 (
  echo.
  echo Kiem tra .NET Framework, SQL Server va thong bao loi o tren.
  pause
  exit /b 1
)
