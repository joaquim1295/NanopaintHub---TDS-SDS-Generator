# Executa o cliente WinUI. Se os pacotes NuGet já estiverem em cache, usa --no-restore.
param(
    [switch]$ForceRestore
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "..\Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj"
$project = [System.IO.Path]::GetFullPath($project)

if ($ForceRestore) {
    Write-Host "A restaurar pacotes NuGet..."
    dotnet restore $project
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore falhou. Verifique ligação a https://api.nuget.org (DNS/rede/VPN)."
    }
}

Write-Host "A compilar (sem restore)..."
dotnet build $project --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build sem restore falhou; a tentar restore + build..."
    dotnet restore $project
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore falhou. Este anfitrião não é conhecido = problema de DNS/rede no PC."
    }
    dotnet build $project
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "A iniciar aplicação (Debug unpackaged)..."
dotnet run --project $project --no-restore -c Debug
