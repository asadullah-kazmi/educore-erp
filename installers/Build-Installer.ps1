param(
    [string]$Configuration = "Release",
    [string]$InnoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"
$installerRoot = $PSScriptRoot
$repoRoot = Split-Path $installerRoot -Parent
$appProject = Join-Path $repoRoot "src\SchoolERP\SchoolERP.csproj"
$databaseProject = Join-Path $installerRoot "DatabaseSetup\SchoolERP.DatabaseSetup.csproj"
$staging = Join-Path $installerRoot "staging"
$appOutput = Join-Path $staging "App"
$databaseOutput = Join-Path $staging "DatabaseSetup"
$scriptsOutput = Join-Path $staging "Database"
$dotNetInstaller = Join-Path $installerRoot "prerequisites\ndp48-x86-x64-allos-enu.exe"
$sqlInstaller = Join-Path $installerRoot "prerequisites\SQLEXPR_x64_ENU.exe"

if (-not (Test-Path $InnoCompiler)) {
    throw "Inno Setup 6 compiler was not found at '$InnoCompiler'. Install Inno Setup or pass -InnoCompiler."
}
if (-not (Test-Path $dotNetInstaller)) {
    throw "Missing .NET Framework installer: $dotNetInstaller"
}
if (-not (Test-Path $sqlInstaller)) {
    throw "Missing SQL Server Express installer: $sqlInstaller"
}

Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $appOutput, $databaseOutput, $scriptsOutput -Force | Out-Null

dotnet build $appProject -c $Configuration -p:OutDir="$appOutput\"
if ($LASTEXITCODE -ne 0) { throw "Application build failed." }

dotnet build $databaseProject -c $Configuration -p:OutDir="$databaseOutput\"
if ($LASTEXITCODE -ne 0) { throw "Database setup build failed." }

Copy-Item (Join-Path $repoRoot "src\SchoolERP\Data\schema.sql") $scriptsOutput
Copy-Item (Join-Path $installerRoot "production_seed.sql") $scriptsOutput

$configPath = Join-Path $appOutput "SchoolERP.exe.config"
[xml]$config = Get-Content -LiteralPath $configPath
$config.configuration.connectionStrings.add.connectionString = "Server=.\SCHOOLERP;Database=SchoolERP;Integrated Security=True;TrustServerCertificate=True;"
$config.Save($configPath)

& $InnoCompiler (Join-Path $installerRoot "SchoolERP.iss")
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }

Write-Host "Installer created in: $(Join-Path $installerRoot 'output')" -ForegroundColor Green
