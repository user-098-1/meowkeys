Unicode true
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUninstDetails show
ManifestDPIAware true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "Sections.nsh"

!define APP "键盘喵"
!define APPFILE "键盘喵.exe"
!define VERSION "1.5.0"
!define UNINSTALL "卸载键盘喵.exe"
!define DEST "$LOCALAPPDATA\Programs\MeowkeysDesktop"
!define MENU "$SMPROGRAMS"
!define DESK "$DESKTOP"
!define PREFS "$LOCALAPPDATA\MeowkeysDesktop\preferences.xml"
!define ARP "Software\Microsoft\Windows\CurrentVersion\Uninstall\MeowkeysDesktop.B48D8B"

Name "${APP}"
Caption "${APP} ${VERSION} 安装"
OutFile "${OUTPUT}"
InstallDir "${DEST}"
BrandingText "${APP} ${VERSION}"
VIProductVersion "1.5.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "${APP}"
VIAddVersionKey /LANG=2052 "FileDescription" "键盘喵 Windows 安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=2052 "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=2052 "LegalCopyright" "键盘喵"

!define MUI_ICON "${CAT_ICON}"
!define MUI_UNICON "${CAT_ICON}"
!define MUI_WELCOMEFINISHPAGE_BITMAP "${WIZARD}"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "${WIZARD}"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "安装键盘喵"
!define MUI_WELCOMEPAGE_TEXT "小猫会在桌面上跟着你移动鼠标、敲键盘。$\r$\n$\r$\n程序安装在当前用户账户下，安装后可以从开始菜单启动。"
!insertmacro MUI_PAGE_WELCOME
!define MUI_COMPONENTSPAGE_TEXT_TOP "选择需要安装的项目，然后单击“安装”。"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TITLE "安装完成"
!define MUI_FINISHPAGE_TEXT "右键点击小猫或任务栏通知区域的猫脸图标，即可选择猫咪、键盘和鼠标样式。"
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APPFILE}"
!define MUI_FINISHPAGE_RUN_TEXT "启动键盘喵"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "SimpChinese"

!macro QuitWithError message
  MessageBox MB_OK|MB_ICONSTOP "${message}" /SD IDOK
  SetErrorLevel 2
  Abort
!macroend
!macro InstallerLock
  ; The kernel releases this per-user-session handle when the installer exits.
  System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Local\MeowkeysDesktop.B48D8B.Setup") p.r0 ?e'
  Pop $1
  ${If} $0 == 0
  ${OrIf} $1 == 183
    !insertmacro QuitWithError "另一个安装或卸载程序正在运行。请等它完成，再重试。"
  ${EndIf}
!macroend
!macro LoadHelper
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File /oname=SetupTasks.exe "${HELPER}"
!macroend
!macro StopPet
  ExecWait '"$PLUGINSDIR\SetupTasks.exe" close "$INSTDIR\${APPFILE}"' $0
  ${If} $0 != 0
    !insertmacro QuitWithError "请从任务栏通知区域的猫脸菜单退出已安装的小猫，然后重试。"
  ${EndIf}
!macroend
!macro ManageLink op folder
  ExecWait '"$PLUGINSDIR\SetupTasks.exe" ${op} "$INSTDIR\${APPFILE}" "${folder}\${APP}.lnk"' $0
  ${If} $0 != 0
    DetailPrint "快捷方式未能更新。可以在安装目录里启动小猫。"
  ${EndIf}
!macroend



Section "桌面应用和开始菜单快捷方式" SEC_APP
  SectionIn RO
  !insertmacro LoadHelper
  SetOutPath "$INSTDIR"
  IfFileExists "$INSTDIR\${APPFILE}" 0 prepare
  ReadRegStr $1 HKCU "${ARP}" "InstallLocation"
  ${If} $1 != "$INSTDIR"
    !insertmacro QuitWithError "安装目录里已有同名文件。请先移走该文件，然后重试。"
  ${EndIf}
  GetDLLVersion "$INSTDIR\${APPFILE}" $0 $1
  ${If} $0 > 0x00010005
    !insertmacro QuitWithError "已安装版本比这个安装包更新。请使用较新的安装包。"
  ${EndIf}
  ${If} $0 == 0x00010005
  ${AndIf} $1 > 0
    !insertmacro QuitWithError "已安装版本比这个安装包更新。请使用较新的安装包。"
  ${EndIf}
  !insertmacro StopPet
  prepare:
  SetOverwrite on
  ClearErrors
  GetTempFileName $3 "$INSTDIR"
  File /oname=$3 "${PAYLOAD}"
  IfErrors write_error
  ; Commit a fully extracted file on the same volume. A failed replacement keeps the old executable.
  System::Call 'kernel32::MoveFileExW(w "$3", w "$INSTDIR\${APPFILE}", i 9) i.r0'
  ${If} $0 == 0
    Delete "$3"
    !insertmacro QuitWithError "无法更新程序。请先退出已安装的小猫，再重新运行安装包。"
  ${EndIf}
  WriteUninstaller "$INSTDIR\${UNINSTALL}"
  WriteRegStr HKCU "${ARP}" "DisplayName" "${APP}"
  WriteRegStr HKCU "${ARP}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "${ARP}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${ARP}" "DisplayIcon" "$INSTDIR\${APPFILE},0"
  WriteRegStr HKCU "${ARP}" "UninstallString" '"$INSTDIR\${UNINSTALL}"'
  WriteRegStr HKCU "${ARP}" "QuietUninstallString" '"$INSTDIR\${UNINSTALL}" /S'
  WriteRegDWORD HKCU "${ARP}" "NoModify" 1
  WriteRegDWORD HKCU "${ARP}" "NoRepair" 1
  !insertmacro ManageLink create-link "${MENU}"
  ExecWait '"$PLUGINSDIR\SetupTasks.exe" first-run "${PREFS}"' $0
  ${If} $0 != 0
    DetailPrint "首次外观未能写入。可从小猫的右键菜单选择样式。"
  ${EndIf}
  Goto done
  write_error:
  Delete "$3"
  !insertmacro QuitWithError "无法写入程序文件。请检查当前用户的安装目录，然后重试。"
  done:
SectionEnd
Section "桌面快捷方式" SEC_DESKTOP
  !insertmacro ManageLink create-link "${DESK}"
  WriteRegDWORD HKCU "${ARP}" "DesktopShortcut" 1
SectionEnd
Section -Finalize
  ${IfNot} ${SectionIsSelected} ${SEC_DESKTOP}
    ReadRegDWORD $0 HKCU "${ARP}" "DesktopShortcut"
    ${If} $0 == 1
      !insertmacro ManageLink remove-link "${DESK}"
    ${EndIf}
    WriteRegDWORD HKCU "${ARP}" "DesktopShortcut" 0
  ${EndIf}
SectionEnd
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_APP} "安装桌宠主程序，并在开始菜单中加入入口。"
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DESKTOP} "在桌面放置一个启动小猫的快捷方式。"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onInit
  !insertmacro InstallerLock
  SetShellVarContext current
  ${If} $INSTDIR != "${DEST}"
    !insertmacro QuitWithError "请从 Windows 的应用列表或应用原安装目录执行卸载。"
  ${EndIf}
FunctionEnd
Section "Uninstall"
  !insertmacro LoadHelper
  !insertmacro StopPet
  ClearErrors
  Delete "$INSTDIR\${APPFILE}"
  ${If} ${Errors}
    !insertmacro QuitWithError "无法删除程序。请退出已安装的小猫，然后重新卸载。"
  ${EndIf}
  !insertmacro ManageLink remove-link "${MENU}"
  !insertmacro ManageLink remove-link "${DESK}"
  Delete "$INSTDIR\${UNINSTALL}"
  DeleteRegKey HKCU "${ARP}"
  SetOutPath "$TEMP"
  RMDir "$INSTDIR"
SectionEnd

Function .onInit
  !insertmacro InstallerLock
  SetShellVarContext current
  StrCpy $INSTDIR "${DEST}"
  ClearErrors
  ReadRegDWORD $0 HKCU "${ARP}" "DesktopShortcut"
  ${IfNot} ${Errors}
  ${AndIf} $0 == 0
    !insertmacro UnselectSection ${SEC_DESKTOP}
  ${EndIf}
  ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  ${If} $0 < 528040
    !insertmacro QuitWithError "此程序需要 .NET Framework 4.8 或更高版本。请先在 Windows 中安装该运行环境，然后重试。"
  ${EndIf}
FunctionEnd
