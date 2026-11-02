Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"

!ifndef APP_VERSION
  !define APP_VERSION "0.1.0"
!endif
!ifndef PUBLISH_DIR
  !define PUBLISH_DIR "..\artifacts\publish"
!endif
!ifndef OUTPUT_FILE
  !define OUTPUT_FILE "..\artifacts\installer\InformesTI-Setup-${APP_VERSION}-win-x64.exe"
!endif

Name "Informes TI"
OutFile "${OUTPUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\InformesTI"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ManifestDPIAware true
ManifestSupportedOS all
BrandingText "Desarrollado por Ing. Luis Ríos, MBA"
VIProductVersion "0.1.0.0"
VIAddVersionKey "ProductName" "Informes TI"
VIAddVersionKey "CompanyName" "Ing. Luis Ríos, MBA"
VIAddVersionKey "LegalCopyright" "Copyright © 2026 Ing. Luis Ríos, MBA"
VIAddVersionKey "FileDescription" "Instalador de Informes TI"
VIAddVersionKey "FileVersion" "${APP_VERSION}"

!define MUI_ICON "..\src\InformesTI.App\Assets\app.ico"
!define MUI_UNICON "..\src\InformesTI.App\Assets\app.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "Bienvenido a Informes TI"
!define MUI_WELCOMEPAGE_TEXT "Informes locales de mantenimiento técnico.$\r$\n$\r$\nSe instalará para tu usuario de Windows, con .NET incluido.$\r$\n$\r$\nDesarrollado por Ing. Luis Ríos, MBA."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\docs\TERMINOS.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\InformesTI.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir Informes TI"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\PRIMEROS-PASOS.txt"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Spanish"

!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\InformesTI"

!macro CheckAppClosed
  System::Call 'kernel32::OpenMutexW(i 0x100000, i 0, w "Local\InformesTI-Desktop-v1") p.r0'
  ${If} $0 != 0
    System::Call 'kernel32::CloseHandle(p r0)'
    MessageBox MB_OK|MB_ICONEXCLAMATION "Cierra Informes TI antes de instalar, actualizar o desinstalar." /SD IDOK
    SetErrorLevel 2
    Abort
  ${EndIf}
!macroend

Function .onInit
  SetShellVarContext current
  SetRegView 64
  ; Ruta fija, independiente de los datos y sin permitir seleccionar una carpeta ajena.
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\InformesTI"
  ${IfNot} ${RunningX64}
    MessageBox MB_OK|MB_ICONSTOP "Esta versión requiere Windows 11 de 64 bits." /SD IDOK
    SetErrorLevel 3
    Abort
  ${EndIf}
  ReadRegStr $0 HKLM "SOFTWARE\Microsoft\Windows NT\CurrentVersion" "CurrentBuildNumber"
  ${If} $0 < 22000
    MessageBox MB_OK|MB_ICONSTOP "Esta versión requiere Windows 11 (compilación 22000 o posterior)." /SD IDOK
    SetErrorLevel 3
    Abort
  ${EndIf}
  System::Call 'kernel32::CreateMutexW(p 0, i 0, w "Local\InformesTI-Setup-v1") p.r1 ?e'
  Pop $0
  ${If} $0 = 183
    MessageBox MB_OK|MB_ICONEXCLAMATION "Ya hay otro instalador de Informes TI abierto." /SD IDOK
    SetErrorLevel 2
    Abort
  ${EndIf}
  !insertmacro CheckAppClosed
FunctionEnd

Section "Informes TI (obligatorio)" SecApp
  SectionIn RO
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  File /r "${PUBLISH_DIR}\*"
  File /oname=PRIMEROS-PASOS.txt "..\docs\PRIMEROS-PASOS.txt"
  WriteUninstaller "$INSTDIR\Desinstalar.exe"
  CreateDirectory "$SMPROGRAMS\Informes TI"
  CreateShortcut "$SMPROGRAMS\Informes TI\Informes TI.lnk" "$INSTDIR\InformesTI.exe"
  CreateShortcut "$SMPROGRAMS\Informes TI\Desinstalar Informes TI.lnk" "$INSTDIR\Desinstalar.exe"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "Informes TI"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "Ing. Luis Ríos, MBA"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\InformesTI.exe"
  WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '$\"$INSTDIR\Desinstalar.exe$\"'
  WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '$\"$INSTDIR\Desinstalar.exe$\" /S'
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  WriteRegDWORD HKCU "${UNINSTALL_KEY}" "EstimatedSize" $0
SectionEnd

Section "Acceso directo en el escritorio" SecDesktop
  CreateShortcut "$DESKTOP\Informes TI.lnk" "$INSTDIR\InformesTI.exe"
SectionEnd

Function un.onInit
  SetShellVarContext current
  SetRegView 64
  ; Se evita cualquier borrado recursivo fuera de la ruta propia de instalación.
  StrCpy $INSTDIR "$LOCALAPPDATA\Programs\InformesTI"
  !insertmacro CheckAppClosed
FunctionEnd

Section "Uninstall"
  SetShellVarContext current
  Delete "$DESKTOP\Informes TI.lnk"
  Delete "$SMPROGRAMS\Informes TI\Informes TI.lnk"
  Delete "$SMPROGRAMS\Informes TI\Desinstalar Informes TI.lnk"
  RMDir "$SMPROGRAMS\Informes TI"
  DeleteRegKey HKCU "${UNINSTALL_KEY}"
  RMDir /r "$INSTDIR"
  ; NO tocar $LOCALAPPDATA\InformesTI: contiene el historial y las fotografías.
SectionEnd
