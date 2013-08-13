; example1.nsi
;
; This script is perhaps one of the simplest NSIs you can make. All of the
; optional settings are left to their default settings. The installer simply 
; prompts the user asking them where to install, and drops a copy of example1.nsi
; there. 

;--------------------------------

; The name of the installer
Name "Bizhawk Prerequisites"

; The file to write
OutFile "bizhawk_prereqs.exe"

; The default installation directory
InstallDir $DESKTOP\Example1

; Request application privileges for Windows Vista+
RequestExecutionLevel admin 

LicenseText "The following prerequisites will be checked and installed:" "OK"
LicenseData "dist\info.txt"
Page license
Page instfiles

Section "Windows Imaging Component (.net 4.0 prerequisite for older OS)" SEC_WIC

  SetOutPath "$TEMP"
  File "dist\wic_x86_enu.exe"
  DetailPrint "Running Windows Imaging Component Setup..."
  ExecWait '"$TEMP\wic_x86_enu.exe" /passive /norestart'
  DetailPrint "Finished Windows Imaging Component Setup"
  
  Delete "$TEMP\wic_x86_enu.exe"

done:
SectionEnd

Section "Microsoft Visual C++ 2010 SP1 Redist" SEC_CRT2010_SP1

  SetOutPath "$TEMP"
  File "dist\vcredist_2010_sp1_x86.exe"
  DetailPrint "Running Visual C++ 2010 SP1 Redistributable Setup..."
  ExecWait '"$TEMP\vcredist_2010_sp1_x86.exe" /passive /notrestart'
  DetailPrint "Finished Visual C++ 2010 SP1 Redistributable Setup"
  
  Delete "$TEMP\vcredist_2010_sp1_x86.exe"

done:
SectionEnd

!define NETVersion "4.0.30319"
!define NETInstaller "dotNetFx40_Full_setup.exe"
Section "MS .NET Framework v${NETVersion}" SecFramework
  IfFileExists "$WINDIR\Microsoft.NET\Framework\v${NETVersion}\mscorlib.dll" NETFrameworkInstalled 0
  File /oname=$TEMP\${NETInstaller} dist\${NETInstaller}
 
  DetailPrint "Starting Microsoft .NET Framework v${NETVersion} Setup..."
  ExecWait '"$TEMP\${NETInstaller}" /passive /norestart'
  Return
 
  NETFrameworkInstalled:
  DetailPrint "Microsoft .NET Framework is already installed!"
 
SectionEnd

Section "DirectX Web Setup" SEC_DIRECTX
                                                                              
 ;SectionIn RO

 SetOutPath "$TEMP"
 File "dist\dxwebsetup.exe"
 DetailPrint "Running DirectX Web Setup..."
 ExecWait '"$TEMP\dxwebsetup.exe" /Q'
 DetailPrint "Finished DirectX Web Setup"                                     
                                                                              
 Delete "$TEMP\dxwebsetup.exe"

SectionEnd