@echo off
setlocal
cd /d "%~dp0"
where powershell.exe >nul 2>&1
if errorlevel 1 (
  echo May can co Windows PowerShell 5.1 de khoi dong Auction Arena.
  pause
  exit /b 1
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0DONG_GOI\CHAY_TU_MA_NGUON.ps1"
if errorlevel 1 (
  echo.
  echo Khong the chay Auction Arena. Xem thong bao loi o tren.
  pause
  exit /b 1
)
endlocal
