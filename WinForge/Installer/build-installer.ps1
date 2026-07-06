<#
.SYNOPSIS
    Publica o WinForge e gera o instalador usando Inno Setup.
.DESCRIPTION
    Executa dotnet publish em modo Release e compila o instalador .exe via Inno Setup.
.PARAMETER SelfContained
    Se especificado, gera build self-contained (inclui runtime .NET 9).
    Sem este parâmetro, gera framework-dependent (requer .NET 9 Runtime no PC do usuário).
.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -SelfContained
#>
param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

# --- Configurações ---
$ProjectDir   = Join-Path $PSScriptRoot ".."
$PublishDir   = Join-Path $ProjectDir "output"
$InstallerDir = $PSScriptRoot
$IssScript    = Join-Path $InstallerDir "WinForge Setup.iss"
$Version      = "1.0.0.0"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  WinForge - Build do Instalador"           -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Verificar Inno Setup ---
$isccPath = $null

# Procurar no PATH
$isccInPath = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
if ($isccInPath) {
    $isccPath = $isccInPath.Source
}

# Procurar nos locais padrão de instalação
if (-not $isccPath) {
    $commonPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 5\ISCC.exe"
    )
    foreach ($p in $commonPaths) {
        if (Test-Path $p) {
            $isccPath = $p
            break
        }
    }
}

if (-not $isccPath) {
    Write-Host "[ERRO] Inno Setup nao encontrado!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Instale o Inno Setup 6 em: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "Ou adicione iscc.exe ao PATH do sistema." -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Inno Setup encontrado: $isccPath" -ForegroundColor Green

# --- Verificar .NET SDK ---
$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "[ERRO] dotnet SDK nao encontrado no PATH!" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] .NET SDK encontrado: $($dotnet.Source)" -ForegroundColor Green
Write-Host ""

# --- Publicar o projeto ---
Write-Host "[1/3] Publicando o WinForge em Release..." -ForegroundColor Yellow

# Limpar diretorio de saida
if (Test-Path $PublishDir) {
    Remove-Item -Recurse -Force $PublishDir
}

$publishArgs = @(
    "publish"
    "`"$ProjectDir\WinForge.csproj`""
    "-c", "Release"
    "-o", "`"$PublishDir`""
    "--nologo"
)

if ($SelfContained) {
    $publishArgs += "-r", "win-x64"
    $publishArgs += "--self-contained", "true"
    Write-Host "  Modo: Self-contained (inclui .NET 9 Runtime)" -ForegroundColor Gray
} else {
    $publishArgs += "--self-contained", "false"
    Write-Host "  Modo: Framework-dependent (requer .NET 9 Runtime)" -ForegroundColor Gray
}

$publishArgs += "-p:Version=$Version"

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao publicar o projeto!" -ForegroundColor Red
    exit 1
}

$publishSize = (Get-ChildItem -Recurse -File $PublishDir | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "[OK] Publicacao concluida ($([math]::Round($publishSize, 2)) MB)" -ForegroundColor Green
Write-Host ""

# --- Compilar o instalador ---
Write-Host "[2/3] Compilando o instalador com Inno Setup..." -ForegroundColor Yellow

# Verificar se o script .iss existe
if (-not (Test-Path $IssScript)) {
    Write-Host "[ERRO] Script Inno Setup nao encontrado: $IssScript" -ForegroundColor Red
    exit 1
}

# Preparar argumentos para o Inno Setup
$isccArgs = @(
    "/F`"WinForge-Setup-$Version`""
    "/O`"$PublishDir`""
    "`"$IssScript`""
)

& $isccPath @isccArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao compilar o instalador!" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Instalador compilado!" -ForegroundColor Green
Write-Host ""

# --- Resultado ---
$setupFile = Join-Path $PublishDir "WinForge-Setup-$Version.exe"
if (Test-Path $setupFile) {
    $setupSize = (Get-Item $setupFile).Length / 1MB
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  Instalador gerado com sucesso!"           -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Arquivo:  $setupFile" -ForegroundColor White
    Write-Host "  Tamanho:  $([math]::Round($setupSize, 2)) MB" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "[ERRO] Arquivo do instalador nao foi encontrado apos compilacao." -ForegroundColor Red
    exit 1
}
