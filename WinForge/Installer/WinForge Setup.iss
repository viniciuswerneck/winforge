; =============================================================================
; WinForge - Inno Setup Script
; Gerador de instalador para o WinForge (Gerenciador de Pacotes Windows)
; =============================================================================
; Prerequisitos: Inno Setup 6+
; Gerar com: iscc.exe "WinForge Setup.iss" ou via build-installer.ps1
; =============================================================================

#define MyAppName      "WinForge"
#define MyAppVersion   "1.0.0.0"
#define MyAppPublisher "Werneck Lab"
#define MyAppURL       "https://werneck.dev"
#define MyAppExeName   "WinForge.exe"

; Caminho relativo para a pasta de publicacao do dotnet
#define PublishDir     "..\output"

[Setup]
; AppId unico (gerar um GUID para producao, mas para v1.0 usamos um fixo)
AppId={{B8F2A4C1-7E3D-4F5A-9A6B-1C2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Diretorio de instalacao (per-user, sem necessidade de admin)
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}

; Icones
SetupIconFile=..\icon.ico

; Visual do instalador
WizardStyle=modern
WizardSizePercent=110

; Saida
OutputDir={#PublishDir}
OutputBaseFilename=WinForge-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
LZMANumBlockThreads=4

; Permissoes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Desinstalador
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; Arquitetura
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Dirio de criacao
CreateUninstallRegKey=yes

[Languages]
Name: "portugues";    MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english";      MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon";  Description: "{cm:CreateDesktopIcon}";  GroupDescription: "{cm:AdditionalIcons}";  Flags: unchecked

[Files]
; Copiar todos os arquivos da publicacao
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Atalho no Menu Iniciar
Name: "{group}\{#MyAppName}";                  Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; Atalho na Area de Trabalho (opcional)
Name: "{autodesktop}\{#MyAppName}";             Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; Verificar .NET 9 Desktop Runtime apos instalacao
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Limpar arquivos criados pelo app
Type: filesandordirs; Name: "{app}"
Type: files;          Name: "{localappdata}\{#MyAppName}\config.json"

[Code]
// =============================================================================
// Verificacao do .NET 9 Desktop Runtime
// =============================================================================

function IsDotNet9DesktopRuntimeInstalled: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TempFile: String;
  ShellResult: Boolean;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet_check.txt');

  // Verificar se o dotnet esta disponivel e se o runtime 9.x esta instalado
  ShellResult := Exec(
    ExpandConstant('{cmd}'),
    '/c dotnet --list-runtimes > "' + TempFile + '" 2>&1',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode
  );

  if ShellResult and (ResultCode = 0) then
  begin
    if LoadStringFromFile(TempFile, Output) then
    begin
      // Procurar por Microsoft.NETCore.App 9.x
      if Pos('Microsoft.WindowsDesktop.App 9.', Output) > 0 then
        Result := True;
    end;
  end;

  // Limpar arquivo temporario
  DeleteFile(TempFile);
end;

function InitializeSetup: Boolean;
var
  DownloadURL: String;
  MsgResult: Integer;
begin
  Result := True;
  DownloadURL := 'https://dotnet.microsoft.com/download/dotnet/9.0';

  if not IsDotNet9DesktopRuntimeInstalled then
  begin
    MsgResult := MsgBox(
      'O .NET 9 Desktop Runtime nao foi encontrado no seu sistema.'#13#10#13#10 +
      'O WinForge precisa deste runtime para funcionar.'#13#10#13#10 +
      'Deseja continuar a instalacao mesmo assim?'#13#10#10 +
      'Voce pode baixar o runtime em:'#13#10 +
      DownloadURL,
      mbConfirmation,
      MB_YESNO
    );

    if MsgResult = IDNO then
    begin
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DownloadURL: String;
begin
  if CurStep = ssPostInstall then
  begin
    DownloadURL := 'https://dotnet.microsoft.com/download/dotnet/9.0';

    if not IsDotNet9DesktopRuntimeInstalled then
    begin
      MsgBox(
        'ATENCAO: O .NET 9 Desktop Runtime ainda nao foi detectado.'#13#10#13#10 +
        'Para usar o WinForge, instale o .NET 9 Desktop Runtime:'#13#10 +
        DownloadURL + #13#10#13#10 +
        'Apos a instalacao do runtime, reinicie o WinForge.',
        mbInformation,
        MB_OK
      );
    end;
  end;
end;
