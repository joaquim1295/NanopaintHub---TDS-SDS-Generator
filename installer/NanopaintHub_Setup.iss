; Script de Instalação Inno Setup para NanopaintHub
; Compilador compatível: Inno Setup 6.x

#define MyAppName "NanopaintHub"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Nanopaint"
#define MyAppURL "https://nanopaint-tech.com/"
#define MyAppExeName "NanopaintHub.WinUIClient.exe"
#define AppIcon "..\Clients\NanopaintHub.WinUiClient\Assets\AppIcon.ico"
#define PublishDir "..\Publish\NanopaintHub-win-x64"

[Setup]
; Identificador exclusivo da aplicação
AppId={{D37F8A1C-9E4B-4E38-B6F1-2F1E0C95A781}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Diretórios de Destino
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Visual e Metadados do Instalador
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\Assets\AppIcon.ico
OutputDir=..\Publish\Installer
OutputBaseFilename=NanopaintHub_Setup_v1.0
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; Requisitos de Sistema Operacional (Windows 10/11 x64)
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Permissões (permite instalar por utilizador ou máquina com diálogo)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Fechar aplicação em execução se estiver aberta
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Todos os binários publicados (Self-Contained)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Ícone da Aplicação garantido na pasta de instalação
Source: "{#AppIcon}"; DestDir: "{app}\Assets"; Flags: ignoreversion

[Icons]
; Menu Iniciar
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Ambiente de Trabalho (opcional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: desktopicon

[Run]
; Opção para executar ao terminar a instalação
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
