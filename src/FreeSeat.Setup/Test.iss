#define MyAppName "TestApp"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Test"
#define MyAppURL "https://example.com"
#define MyAppExeName "FreeSeat.App.exe"

[Setup]
AppId={{12345678-1234-1234-1234-123456789012}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
OutputDir=..\..\dist
OutputBaseFilename=TestApp-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041
PrivilegesRequired=admin

[Files]
Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\publish\FreeSeat.App.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent