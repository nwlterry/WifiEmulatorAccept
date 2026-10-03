#ifndef DotNetMajor
  #define DotNetMajor "10"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish\net10.0-windows"
#endif

#define MyAppName "Wi-Fi Emulator Accept"
#define MyAppFullName "Wi-Fi Emulator Accept (.NET " + DotNetMajor + ")"
#define MyAppVersion "1.1.0"
#define MyAppExeName "WifiEmulatorAccept.exe"

[Setup]
AppId={{B7E4A1C2-6F58-4D3A-9C21-8E5F0A7B4D16}
AppName={#MyAppFullName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppName}
DefaultDirName={localappdata}\Programs\WifiEmulatorAccept
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
OutputDir=..\dist
OutputBaseFilename=WifiEmulatorAccept-Setup-net{#DotNetMajor}
SetupIconFile=..\App.ico
UninstallDisplayIcon={app}\App.ico
UninstallDisplayName={#MyAppFullName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
MinVersion=10.0
VersionInfoVersion=1.1.0.0
VersionInfoDescription=Pure .NET program for Windows only. Setup for the .NET {#DotNetMajor} Windows Desktop Runtime.
VersionInfoProductName={#MyAppFullName}
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[InstallDelete]
Type: files; Name: "{autoprograms}\Wi-Fi Emulator Accept.lnk"
Type: files; Name: "{autodesktop}\Wi-Fi Emulator Accept.lnk"
Type: files; Name: "{autoprograms}\Wi-Fi Emulator Accept (.NET 8).lnk"
Type: files; Name: "{autodesktop}\Wi-Fi Emulator Accept (.NET 8).lnk"
Type: files; Name: "{autoprograms}\Wi-Fi Emulator Accept (.NET 10).lnk"
Type: files; Name: "{autodesktop}\Wi-Fi Emulator Accept (.NET 10).lnk"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\WifiEmulatorAccept.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\WifiEmulatorAccept.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\WifiEmulatorAccept.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\App.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Help.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\App.ico"; Comment: "Pure .NET program for Windows only. Clicks Allow or Accept when Wi-Fi reconnects. .NET {#DotNetMajor} Windows Desktop Runtime."
Name: "{autodesktop}\{#MyAppFullName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\App.ico"; Comment: "Pure .NET program for Windows only. Clicks Allow or Accept when Wi-Fi reconnects. .NET {#DotNetMajor} Windows Desktop Runtime."; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function DesktopRuntimePresent(const Root: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(AddBackslash(Root) + '{#DotNetMajor}.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;
end;

function DotNetDesktopInstalled: Boolean;
begin
  Result :=
    DesktopRuntimePresent(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App')) or
    DesktopRuntimePresent(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App'));
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not DotNetDesktopInstalled then
  begin
    MsgBox('This package is a pure .NET program for Windows only.' + #13#10 +
      'It needs the .NET {#DotNetMajor} Windows Desktop Runtime.' + #13#10 +
      'Install that runtime, then run this setup again.', mbError, MB_OK);
    Result := False;
  end;
end;
