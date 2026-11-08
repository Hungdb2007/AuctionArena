Unicode true
!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"
!include "StrFunc.nsh"
${StrStr}

!ifndef PAYLOAD
  !define PAYLOAD "SETUP_PAYLOAD"
!endif
Name "Auction Arena"
OutFile "..\AuctionArena_Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\AuctionArena"
InstallDirRegKey HKCU "Software\AuctionArena" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "Auction Arena"
VIAddVersionKey "FileDescription" "Cài đặt Auction Arena"
VIAddVersionKey "FileVersion" "1.0.0"
VIAddVersionKey "LegalCopyright" "Auction Arena"

Var SqlServer
Var ServerInput
Var CreateCheckbox
Var CreateDatabase
Var Dialog
Var DbReady
Var FinishText

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom DatabasePage DatabasePageLeave
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "$FinishText"
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_TEXT "Mở Auction Arena"
!define MUI_FINISHPAGE_RUN_FUNCTION LaunchApplication
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "Vietnamese"

Function .onInit
  SetShellVarContext current
  IfSilent 0 +3
    MessageBox MB_ICONSTOP "Hãy mở bộ cài ở chế độ tương tác để chọn đúng SQL Server."
    Abort
  ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  ${If} $0 < 528040
    MessageBox MB_ICONSTOP "Máy cần .NET Framework 4.8 trước khi cài Auction Arena."
    Abort
  ${EndIf}
  ReadRegStr $SqlServer HKCU "Software\AuctionArena" "SqlServer"
  StrCpy $CreateDatabase ${BST_CHECKED}
  StrCpy $DbReady 1
FunctionEnd

Function DatabasePage
  !insertmacro MUI_HEADER_TEXT "Cấu hình SQL Server" "Chọn máy chủ lưu dữ liệu đấu giá"
  nsDialogs::Create 1018
  Pop $Dialog
  ${If} $Dialog == error
    Abort
  ${EndIf}
  ${NSD_CreateLabel} 0 0 100% 30u "SQL Server phải được cài và đang chạy. Nhập tên instance, ví dụ .\SQLEXPRESS hoặc MAYCHU\SQLEXPRESS."
  Pop $0
  ${NSD_CreateText} 0 34u 100% 14u "$SqlServer"
  Pop $ServerInput
  ${NSD_CreateCheckbox} 0 58u 100% 26u "Tự tạo AuctionArenaDB nếu chưa có bằng Windows Authentication"
  Pop $CreateCheckbox
  ${NSD_SetState} $CreateCheckbox $CreateDatabase
  ${NSD_CreateLabel} 0 90u 100% 40u "Database hiện có được kiểm tra cấu trúc, không xóa dữ liệu. Nếu dùng SQL Authentication, bỏ chọn mục trên rồi cấu hình kết nối trong ứng dụng."
  Pop $0
  nsDialogs::Show
FunctionEnd

Function DatabasePageLeave
  ${NSD_GetText} $ServerInput $SqlServer
  ${NSD_GetState} $CreateCheckbox $CreateDatabase
  ${If} $CreateDatabase == ${BST_CHECKED}
    ${If} $SqlServer == ""
      MessageBox MB_ICONEXCLAMATION "Hãy nhập tên SQL Server."
      Abort
    ${EndIf}
    ; Chỉ cho phép ký tự tên máy/instance/port, không đưa dấu nháy vào PowerShell.
    StrCpy $1 0
    loop:
      StrCpy $2 $SqlServer 1 $1
      StrCmp $2 "" valid
      ${StrStr} $3 "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789._\,-:()" "$2"
      StrCmp $3 "" invalid
      IntOp $1 $1 + 1
      Goto loop
    invalid:
      MessageBox MB_ICONEXCLAMATION "Tên SQL Server chứa ký tự không hợp lệ."
      Abort
    valid:
  ${EndIf}
  IfFileExists "$INSTDIR\AuctionArena.WinForms.exe" 0 unlocked
  System::Call 'kernel32::CreateFileW(w "$INSTDIR\AuctionArena.WinForms.exe", i 0x40000000, i 0, p 0, i 3, i 0, p 0) p.r0'
  ${If} $0 == -1
    MessageBox MB_ICONEXCLAMATION "Hãy đóng Auction Arena và kiểm tra quyền ghi thư mục cài đặt trước khi tiếp tục."
    Abort
  ${EndIf}
  System::Call 'kernel32::CloseHandle(p r0)'
  unlocked:
FunctionEnd

Section "Auction Arena" SEC_APP
  SectionIn RO
  SetOutPath "$INSTDIR"
  SetOverwrite on
  File /r /x "AuctionArena.WinForms.exe.config" "${PAYLOAD}\*.*"
  SetOverwrite off
  File "${PAYLOAD}\AuctionArena.WinForms.exe.config"
  SetOverwrite on
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\Auction Arena"
  CreateShortcut "$SMPROGRAMS\Auction Arena\Auction Arena.lnk" "$INSTDIR\AuctionArena.WinForms.exe"
  CreateShortcut "$SMPROGRAMS\Auction Arena\Cấu hình database.lnk" "$INSTDIR\CAI_DATABASE.cmd"
  CreateShortcut "$SMPROGRAMS\Auction Arena\Gỡ cài đặt.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\Auction Arena.lnk" "$INSTDIR\AuctionArena.WinForms.exe"
  WriteRegStr HKCU "Software\AuctionArena" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\AuctionArena" "SqlServer" "$SqlServer"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "DisplayName" "Auction Arena"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "DisplayVersion" "1.0.0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "DisplayIcon" "$INSTDIR\AuctionArena.WinForms.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena" "NoRepair" 1
  StrCpy $FinishText "Đã cài Auction Arena. Mở ứng dụng để tạo tài khoản quản trị lần đầu và bắt đầu sử dụng."
  ${If} $CreateDatabase == ${BST_CHECKED}
    DetailPrint "Đang chuẩn bị database trên $SqlServer..."
    nsExec::ExecToLog '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\CAI_DATABASE.ps1" -Server "$SqlServer" -NonInteractive -ConfigureApp -LogFile "$INSTDIR\CAI_DATABASE.log"'
    Pop $0
    ${If} $0 != 0
      StrCpy $DbReady 0
      StrCpy $FinishText "Đã cài tệp ứng dụng nhưng database chưa sẵn sàng. Kiểm tra SQL Server, quyền truy cập và CAI_DATABASE.log trong thư mục cài đặt. Dùng mục Cấu hình database trong Start Menu để thử lại."
      MessageBox MB_ICONEXCLAMATION "$FinishText"
      SetErrorLevel 2
    ${EndIf}
  ${EndIf}
SectionEnd

Function LaunchApplication
  ${If} $DbReady == 1
    Exec '"$INSTDIR\AuctionArena.WinForms.exe"'
  ${Else}
    MessageBox MB_ICONINFORMATION "Hãy cấu hình database thành công rồi mở Auction Arena từ Desktop."
  ${EndIf}
FunctionEnd

Function un.onInit
  SetShellVarContext current
  MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Gỡ ứng dụng Auction Arena? Database SQL Server, cấu hình kết nối và nhật ký được giữ lại." IDOK +2
    Abort
  IfFileExists "$INSTDIR\AuctionArena.WinForms.exe" 0 ready
  System::Call 'kernel32::CreateFileW(w "$INSTDIR\AuctionArena.WinForms.exe", i 0x40000000, i 0, p 0, i 3, i 0, p 0) p.r0'
  ${If} $0 == -1
    MessageBox MB_ICONEXCLAMATION "Hãy đóng Auction Arena trước khi gỡ cài đặt."
    Abort
  ${EndIf}
  System::Call 'kernel32::CloseHandle(p r0)'
  ready:
FunctionEnd

Section "Uninstall"
  ; Danh sách được sinh từ payload. Không xóa đệ quy thư mục dữ liệu người dùng.
  !include "UninstallFiles.nsh"
  Delete "$DESKTOP\Auction Arena.lnk"
  Delete "$SMPROGRAMS\Auction Arena\Auction Arena.lnk"
  Delete "$SMPROGRAMS\Auction Arena\Cấu hình database.lnk"
  Delete "$SMPROGRAMS\Auction Arena\Gỡ cài đặt.lnk"
  RMDir "$SMPROGRAMS\Auction Arena"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\AuctionArena"
  DeleteRegKey HKCU "Software\AuctionArena"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
SectionEnd
