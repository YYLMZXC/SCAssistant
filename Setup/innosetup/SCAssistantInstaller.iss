[Setup]
AppName=生存战争助手
AppVersion=1.0
DefaultDirName={pf}\SCAssistant
DefaultGroupName=生存战争助手
OutputBaseFilename=生存战争助手安装程序
Compression=lzma
SolidCompression=yes

[Tasks]
Name: desktopicon; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式"; Flags: unchecked

[Files]
Source: "C:\SC-DEV\GPS\SCAssistant\bin\Debug\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SCAssistant"; Filename: "{app}\SCAssistant.exe"
Name: "{commondesktop}\SCAssistant"; Filename: "{app}\SCAssistant.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SCAssistant.exe"; Description: "启动 SCAssistant"; Flags: nowait postinstall skipifsilent

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"