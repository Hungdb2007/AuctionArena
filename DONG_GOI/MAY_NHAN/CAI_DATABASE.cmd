@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CAI_DATABASE.ps1"
if errorlevel 1 (
  echo.
  echo Khong tao duoc database. Xem thong bao o tren.
) else (
  echo.
  echo Hoan tat kiem tra / tao database.
)
pause
