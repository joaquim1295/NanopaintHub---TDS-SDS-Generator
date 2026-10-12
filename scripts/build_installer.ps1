<#
.SYNOPSIS
    Script automatizado de compilação, publicação e geração do Instalador (Setup) do NanopaintHub.

.DESCRIPTION
    1. Realiza o publish do cliente WinUI em modo Release (x64, Self-Contained, Unpackaged).
    2. Garante a cópia dos Assets e base inicial (nanopaint.initial.db).
    3. Executa o Inno Setup Compiler (ISCC.exe) para criar o executável final NanopaintHub_Setup_v1.0.exe.

.PARAMETER SkipPublish
    Se especificado, pula a etapa de compilação 'dotnet publish' e gera o instalador diretamente a partir da pasta publish existente.

.EXAMPLE
    .\scripts\build_installer.ps1
    .\scripts\build_installer.ps1 -SkipPublish
#>
param(
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$clientProject = Join-Path $projectRoot "Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj"
$issPath = Join-Path $projectRoot "installer\NanopaintHub_Setup.iss"
$innoExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " NanopaintHub - Gerador de Instalador Oficial (Setup)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Verificar Inno Setup
if (-not (Test-Path $innoExe)) {
    Write-Error "Inno Setup 6 nao encontrado em '$innoExe'. Instale o Inno Setup 6 para compilar o instalador."
}

# 2. Publicar projeto via dotnet
if (-not $SkipPublish) {
    Write-Host "`n[1/2] Compilando e publicando binarios (Release win-x64)..." -ForegroundColor Yellow
    
    $targetPublishDir = Join-Path $projectRoot "Publish\NanopaintHub-win-x64"
    if (Test-Path $targetPublishDir) {
        Write-Host "Limpando pasta de publicacao anterior..." -ForegroundColor DarkGray
        Remove-Item -Recurse -Force $targetPublishDir
    }
    $publishArgs = @(
        "publish", $clientProject,
        "-c", "Release",
        "-r", "win-x64",
        "--self-contained", "true",
        "-p:WindowsPackageType=None",
        "-p:WindowsAppSDKSelfContained=true",
        "-p:Platform=x64",
        "-o", $targetPublishDir
    )

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha na compilacao/publicacao do dotnet (Exit code $LASTEXITCODE)."
    }
    Write-Host "Publicacao concluida com sucesso." -ForegroundColor Green
} else {
    Write-Host "`n[1/2] Pulando publish (--SkipPublish selecionado)." -ForegroundColor DarkGray
}

# 3. Compilar instalador
Write-Host "`n[2/2] Gerando instalador executavel via Inno Setup..." -ForegroundColor Yellow
& $innoExe $issPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao do Inno Setup (Exit code $LASTEXITCODE)."
}

$outputExe = Join-Path $projectRoot "Publish\Installer\NanopaintHub_Setup_v1.0.exe"
if (Test-Path $outputExe) {
    $item = Get-Item $outputExe
    $sizeMb = [math]::Round($item.Length / 1MB, 2)
    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host " SUCESSO! Instalador gerado:" -ForegroundColor Green
    Write-Host " Ficheiro: $($item.FullName)" -ForegroundColor White
    Write-Host " Tamanho:  $sizeMb MB" -ForegroundColor White
    Write-Host " Data:     $($item.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Warning "Instalador compilado, mas o ficheiro nao foi encontrado no caminho esperado."
}
