@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0TAO_BAN_CHAY.ps1"
if errorlevel 1 (
  echo.
  echo Khong tao duoc ban chay. Hay xem dong loi o tren.
  pause
  exit /b 1
)
echo.
echo Goi ban chay: AuctionArena_BanChay.zip
pause
