#define AppName "School ERP"
#define AppVersion "1.1.1"
#define AppPublisher "Grammar Public School"
#define AppExeName "SchoolERP.exe"

[Setup]
AppId={{E6889769-E6E2-4C23-9C9F-B5BD906EC72A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\School ERP
DefaultGroupName=School ERP
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=SchoolERP-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}
SetupLogging=yes
RestartIfNeededByRun=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "staging\App\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\DatabaseSetup\*"; DestDir: "{app}\Installer\DatabaseSetup"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\Database\*"; DestDir: "{app}\Installer\Database"; Flags: ignoreversion
Source: "prerequisites\ndp48-x86-x64-allos-enu.exe"; Flags: dontcopy
Source: "prerequisites\SQLEXPR_x64_ENU.exe"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\School ERP"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\School ERP"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\Installer\DatabaseSetup\SchoolERP.DatabaseSetup.exe"; Parameters: """.\SCHOOLERP"" ""{app}\Installer\Database\schema.sql"" ""{app}\Installer\Database\production_seed.sql"""; StatusMsg: "Creating and configuring the SchoolERP database..."; Flags: runhidden waituntilterminated
Filename: "{app}\{#AppExeName}"; Description: "Launch School ERP"; Flags: nowait postinstall skipifsilent

[Code]
const
  DotNet48Release = 528040;

function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
    and (Release >= DotNet48Release);
end;

function IsSqlExpressInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\MSSQL$SCHOOLERP');
end;

function RunPrerequisite(const FileName, Parameters, DisplayName: String): Boolean;
var
  ResultCode: Integer;
begin
  ExtractTemporaryFile(FileName);
  WizardForm.StatusLabel.Caption := 'Installing ' + DisplayName + '...';
  Result := Exec(ExpandConstant('{tmp}\' + FileName), Parameters, '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode) and ((ResultCode = 0) or (ResultCode = 3010));
  if not Result then
    MsgBox(DisplayName + ' installation failed. Exit code: ' + IntToStr(ResultCode), mbError, MB_OK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';

  if not IsDotNet48Installed then
  begin
    if not RunPrerequisite('ndp48-x86-x64-allos-enu.exe', '/q /norestart', '.NET Framework 4.8') then
    begin
      Result := '.NET Framework 4.8 could not be installed.';
      exit;
    end;
  end;

  if not IsSqlExpressInstalled then
  begin
    if not RunPrerequisite('SQLEXPR_x64_ENU.exe',
      '/ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SCHOOLERP ' +
      '/SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE" ' +
      '/SQLSYSADMINACCOUNTS="BUILTIN\ADMINISTRATORS" ' +
      '/IACCEPTSQLSERVERLICENSETERMS /TCPENABLED=0 /NPENABLED=0 /QUIET',
      'SQL Server Express') then
    begin
      Result := 'SQL Server Express could not be installed.';
      exit;
    end;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := IsWin64;
  if not Result then
    MsgBox('School ERP requires 64-bit Windows.', mbError, MB_OK);
end;
