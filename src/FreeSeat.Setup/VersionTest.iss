#define MyAppName "VersionTest"
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
OutputBaseFilename=VersionTest-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0.19041
PrivilegesRequired=admin
VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoDescription={#MyAppName} Test
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) 2024 {#MyAppPublisher}

[Files]
Source: "..\FreeSeat.App\bin\Release\net9.0-windows\win-x64\publish\FreeSeat.App.exe"; DestDir: "{app}"; Flags: ignoreversion