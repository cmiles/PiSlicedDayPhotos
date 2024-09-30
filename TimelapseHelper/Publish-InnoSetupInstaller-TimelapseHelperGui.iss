#ifndef Version
  #define Version = '1902-07-02-00-00-00';
#endif

#ifndef GitCommit
  #define GitCommit = '???';
#endif

#define MyAppPublisher "Charles Miles"
#define MyAppOutputDir "M:\PointlessWaymarksPublications\"

#define MyAppDefaultGroupName "Pi Sliced-Day Photos"

#define MyAppName "Pi Sliced-Day Photos Timelapse Helper"
#define MyAppDefaultDirName "PiSlicedDayTimelapseHelper"
#define MyAppExeName "PiSlicedDayPhotos.TimelapseHelperGui.exe"
#define MyAppOutputBaseFilename "PiSlicedDayPhotos-TimelapseHelperGui-Setup--"
#define MyAppFilesSource "M:\PointlessWaymarksPublications\PiSlicedDayPhotos.TimelapseHelperGui\*"

[Setup]
AppId={{c0d70534-5beb-4e90-b6ba-768e125b6279}
AppName={#MyAppName}
AppVersion={#Version}
AppPublisher={#MyAppPublisher}
WizardStyle=modern
DefaultDirName={autopf}\{#MyAppDefaultDirName}
DefaultGroupName={#MyAppDefaultGroupName}
Compression=lzma2
SolidCompression=yes
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppOutputBaseFilename}{#Version}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest

[Files]
Source: {#MyAppFilesSource}; DestDir: "{app}\"; Flags: recursesubdirs ignoreversion; AfterInstall:PublishVersionAfterInstall;

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}";

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch application"; Flags: postinstall nowait skipifsilent

[Code]
procedure PublishVersionAfterInstall();
begin
  SaveStringToFile(ExpandConstant('{app}\PublishVersion--{#Version}.txt'), ExpandConstant('({#GitCommit})'), False);
end;