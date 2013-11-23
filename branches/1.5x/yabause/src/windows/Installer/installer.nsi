;Yabause Installer
;--------------------------------
!Include 'MUI.nsh'

;Define variable
!define PROGNAME "Yabause"

; The name of the installer
Name "${PROGNAME}"

; The file to write
; Can also be setup via command-line. E.g. makensis /XOutFile setup.exe
;OutFile "Setup.exe"

SetPluginUnload  alwaysoff

; The user doesn't need to see the details
ShowInstDetails "nevershow"
ShowUninstDetails "nevershow"

; GUI variables
XPStyle on
BrandingText " " ; Remove 'Nullsoft Install System vX.XX' text

SetFont "MS Shell Dlg" 9

; The default installation directory
InstallDir "$PROGRAMFILES\${PROGNAME}"

; Registry key to check for directory (so if you install again, it will 
; overwrite the old one automatically)
InstallDirRegKey HKLM "Software\${PROGNAME}" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel user

;--------------------------------
; Pages

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "COPYING.rtf"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_FINISHPAGE_RUN "$INSTDIR\Yabause.exe"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; The stuff to install
Section "${PROGNAME} Core Files"
  SectionIn RO

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Install list
  File "..\..\Yabause.exe"
  File "..\..\..\AUTHORS"
  File "..\..\..\ChangeLog"
  File "..\..\..\COPYING"
  File "..\Release\glut32.dll"
  File "..\..\..\README"
  File "..\..\..\README.WIN"
  
  ; Write the installation path into the registry
  WriteRegStr HKLM "Software\${PROGNAME}" "Install_Dir" "$INSTDIR"
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROGNAME}" "DisplayName" "${PROGNAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROGNAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROGNAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROGNAME}" "NoRepair" 1
  WriteUninstaller "uninstall.exe"

  ; Create Program shortcut
  CreateDirectory "$SMPROGRAMS\${PROGNAME}"
  CreateShortCut "$SMPROGRAMS\${PROGNAME}\${PROGNAME}.lnk" "$INSTDIR\Yabause.exe" "" "$INSTDIR\Yabause.exe" 0
  CreateShortCut "$SMPROGRAMS\${PROGNAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
SectionEnd ; end the section

Section "Create Desktop Shortcut"
  CreateShortCut "$DESKTOP\${PROGNAME}.lnk" "$INSTDIR\Yabause.exe" ""
SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROGNAME}"
  DeleteRegKey HKLM "Software\${PROGNAME}"

  ; Remove files and uninstaller
  Delete $INSTDIR\Yabause.exe
  Delete $INSTDIR\AUTHORS
  Delete $INSTDIR\ChangeLog
  Delete $INSTDIR\COPYING
  Delete $INSTDIR\glut32.dll
  Delete $INSTDIR\README
  Delete $INSTDIR\README.WIN

  ; Remove shortcuts, if any
  Delete "$DESKTOP\${PROGNAME}.lnk"
  Delete "$SMPROGRAMS\${PROGNAME}\*.*"

  ; Remove directories used
  RMDir "$SMPROGRAMS\${PROGNAME}"
  RMDir "$INSTDIR"

SectionEnd
