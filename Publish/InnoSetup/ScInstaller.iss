[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ch"; MessagesFile: "compiler:ChineseSimplified.isl"

[CustomMessages]
en.DesktopIconGroup=Shortcuts
en.DesktopIcon=Create a desktop shortcut
en.SurvivalcraftIconName=SurvivalcraftAssistant
en.StartSurvivalcraft=Start SurvivalcraftAssistant
en.DefaultGroupName=SurvivalcraftAssistant

ch.DesktopIconGroup=快捷方式
ch.DesktopIcon=创建桌面快捷方式
ch.SurvivalcraftIconName=生存战争助手
ch.StartSurvivalcraft=启动 "生存战争助手"
ch.DefaultGroupName=生存战争助手

[Setup]
AppName=SurvivalcraftAssistant
AppVersion=2.4.0
DefaultDirName={autopf}\SurvivalcraftAssistant
DefaultGroupName={cm:DefaultGroupName}
OutputDir=..\Desktop\
OutputBaseFilename=SurvivalcraftAssistant
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Tasks]
Name: desktopicon; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:DesktopIconGroup}"; Flags:

[Files]
Source: "..\Desktop\Content\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{cm:SurvivalcraftIconName}"; Filename: "{app}\Assistant.exe"
Name: "{commondesktop}\{cm:SurvivalcraftIconName}"; Filename: "{app}\Assistant.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Assistant.exe"; Description: "{cm:StartSurvivalcraft}"; Flags: postinstall skipifsilent runascurrentuser nowait
