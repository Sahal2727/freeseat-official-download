; FreeSeat per-user installer. Build with build-tools/Build-Setup.ps1.

#define MyAppName "FreeSeat"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sahal"
#define MyAppURL "https://github.com/freeseat/freeseat"
#define MyAppExeName "FreeSeat.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\artifacts\FreeSeat-Setup
OutputBaseFilename=FreeSeat-Setup
SetupIconFile=..\..\assets\branding\FreeSeat-monitor-icon.ico
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern dynamic
WizardImageFile=..\..\assets\branding\FreeSeat-Setup-Light.png
WizardImageFileDynamicDark=..\..\assets\branding\FreeSeat-Setup-Dark.png
WizardSmallImageFile=..\..\assets\branding\FreeSeat-SetupSmall-Light.png
WizardSmallImageFileDynamicDark=..\..\assets\branding\FreeSeat-SetupSmall-Dark.png
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041
PrivilegesRequired=lowest
UsePreviousAppDir=yes
CreateAppDir=yes
DisableDirPage=no
DisableReadyPage=no
DisableFinishedPage=no
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} - Two seats. One PC.
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"; Flags: checkedonce
Name: "startwithwindows"; Description: "Start FreeSeat with Windows"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "..\..\artifacts\FreeSeat-OneFile\FreeSeat.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch FreeSeat"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#MyAppName}"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startwithwindows
