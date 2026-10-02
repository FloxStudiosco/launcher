#ifndef AppVersion
  #error Pass the launcher version: ISCC /DAppVersion=0.1.0 /DSourceExe=<path to FloxLauncher.exe>
#endif
#ifndef SourceExe
  #error Pass the launcher exe: ISCC /DSourceExe=<path to FloxLauncher.exe>
#endif
#ifndef OutputDir
  #define OutputDir "..\out\installer"
#endif

#define AppName "Kintsugi Master"
#define GameId "KintsugiMaster"

[Setup]
AppId={{8F3C1A47-6B2E-4D59-9A0C-2C7E5B41D3F8}
AppName={#AppName} Launcher
AppVersion={#AppVersion}
AppVerName={#AppName} Launcher {#AppVersion}
AppPublisher=Flox Studios
VersionInfoVersion={#AppVersion}
DefaultDirName={userpf}\Flox Studios\{#AppName}
DisableDirPage=yes
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex=FloxLauncher-{#GameId}
CloseApplications=yes
OutputDir={#OutputDir}
OutputBaseFilename=KintsugiMaster-Setup-{#AppVersion}
UninstallDisplayIcon={app}\FloxLauncher.exe
SetupIconFile=..\src\Launcher.App\Assets\KintsugiMaster.ico
UninstallDisplayName={#AppName} Launcher
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; DestName: "FloxLauncher.exe"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\FloxLauncher.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\FloxLauncher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FloxLauncher.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{localappdata}\FloxStudios\{#GameId}"
