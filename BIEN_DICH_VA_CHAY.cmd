@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0DONG_GOI\CHAY_TU_MA_NGUON.ps1" -Rebuild
if errorlevel 1 (
  echo.
  echo Khong the bien dich va chay Auction Arena. Xem thong bao loi o tren.
  pause
  exit /b 1
)
endlocal
