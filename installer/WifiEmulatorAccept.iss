#define MyAppName "Wi-Fi Emulator Accept"
#define MyAppVersion "1.0.0"
#define MyAppExeName "WifiEmulatorAccept.exe"

[Setup]
AppId={{B7E4A1C2-6F58-4D3A-9C21-8E5F0A7B4D16}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppName}
DefaultDirName={localappdata}\Programs\WifiEmulatorAccept
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=no
OutputDir=..\dist
OutputBaseFilename=WifiEmulatorAccept-Setup
SetupIconFile=..\App.ico
UninstallDisplayIcon={app}\App.ico
UninstallDisplayName={#MyAppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
MinVersion=10.0
VersionInfoVersion=1.0.0.0
VersionInfoDescription=Wi-Fi Emulator Accept Setup
VersionInfoProductName={#MyAppName}
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\WifiEmulatorAccept.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\WifiEmulatorAccept.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\WifiEmulatorAccept.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\App.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\Help.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\App.ico"; Comment: "Accept the emulator prompt when Wi-Fi reconnects"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\App.ico"; Comment: "Accept the emulator prompt when Wi-Fi reconnects"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function DesktopRuntime10Present(const Root: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(AddBackslash(Root) + '10.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;
end;

function DotNetDesktop10Installed: Boolean;
begin
  Result :=
    DesktopRuntime10Present(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App')) or
    DesktopRuntime10Present(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App'));
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not DotNetDesktop10Installed then
  begin
    MsgBox('This program needs the .NET 10 Windows Desktop Runtime.' + #13#10 +
      'Install that runtime, then run this setup again.', mbError, MB_OK);
    Result := False;
  end;
end;
