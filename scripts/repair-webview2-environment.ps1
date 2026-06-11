#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Repara ambiente WebView2/NuGet e permissões Windows para NanopaintHub.
  Executar como Administrador. WebView2 embebido pode continuar indisponível neste PC;
  o editor Quill via Chromium (Puppeteer) é a alternativa recomendada.
#>
param(
    # scripts\ -> raiz do repo NanopaintHub
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
$logDir = Join-Path $env:LOCALAPPDATA "NanopaintHub\Logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$report = Join-Path $logDir "repair-webview2-report.txt"
"" | Set-Content -Path $report -Encoding utf8

function Log($msg) {
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $report -Value $line -Encoding utf8
}

Log "=== NanopaintHub repair-webview2-environment ==="
Log "Repo: $RepoRoot"

$slnPath = Join-Path $RepoRoot "NanopaintHub.sln"
if (-not (Test-Path $slnPath)) {
    Log "ERRO: NanopaintHub.sln nao encontrado em $RepoRoot"
    Log "Use: -RepoRoot `"C:\caminho\para\NanopaintHub`""
    exit 1
}

# 1. Terminar processos presos
Log "A terminar processos msedgewebview2 / NanopaintHub..."
Get-Process -Name "msedgewebview2","NanopaintHub.WinUIClient" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# 2. Limpar cache UDF WebView2 (não remove dados da app)
$wv2Cache = Join-Path $env:LOCALAPPDATA "NanopaintHub\WebView2"
if (Test-Path $wv2Cache) {
    Log "A limpar cache WebView2 UDF: $wv2Cache"
    Remove-Item -Recurse -Force $wv2Cache -ErrorAction SilentlyContinue
}

# 3. NuGet restore + build
Log "dotnet restore solução..."
Push-Location $RepoRoot
try {
    dotnet restore $slnPath 2>&1 | ForEach-Object { Log $_ }
    dotnet build Clients\NanopaintHub.WinUiClient\NanopaintHub.WinUIClient.csproj -c Debug -p:Platform=x64 2>&1 | ForEach-Object { Log $_ }
}
finally {
    Pop-Location
}

# 4. Reinstalar WebView2 Runtime Evergreen
Log "A reinstalar Microsoft Edge WebView2 Runtime (winget)..."
$winget = Get-Command winget -ErrorAction SilentlyContinue
if ($winget) {
    winget install --id Microsoft.EdgeWebView2Runtime --accept-package-agreements --accept-source-agreements --force 2>&1 | ForEach-Object { Log $_ }
}
else {
    Log "winget não encontrado; instale manualmente: https://go.microsoft.com/fwlink/p/?LinkId=2124703"
}

# 5. Reinstalar Edge (opcional, alguns PCs corrigem WebView2)
if ($winget) {
    Log "A verificar Microsoft Edge..."
    winget upgrade --id Microsoft.Edge --accept-package-agreements --accept-source-agreements 2>&1 | ForEach-Object { Log $_ }
}

# 6. Firewall — permitir app e runtime local
$exe = Join-Path $RepoRoot "Clients\NanopaintHub.WinUiClient\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\NanopaintHub.WinUIClient.exe"
if (-not (Test-Path $exe)) {
    $exe = Join-Path $RepoRoot "Clients\NanopaintHub.WinUiClient\bin\Debug\net9.0-windows10.0.26100.0\win-x64\NanopaintHub.WinUIClient.exe"
}

if (Test-Path $exe) {
    Log "Firewall: regra NanopaintHub WinUI ($exe)"
    netsh advfirewall firewall delete rule name="NanopaintHub WinUI" 2>$null | Out-Null
    netsh advfirewall firewall add rule name="NanopaintHub WinUI" dir=out action=allow program="$exe" enable=yes | ForEach-Object { Log $_ }
    netsh advfirewall firewall add rule name="NanopaintHub WinUI In" dir=in action=allow program="$exe" enable=yes | ForEach-Object { Log $_ }
}

$edgeWv2 = "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
if (Test-Path $edgeWv2) {
    Get-ChildItem $edgeWv2 -Recurse -Filter "msedgewebview2.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 | ForEach-Object {
        Log "Firewall: msedgewebview2 $($_.FullName)"
        netsh advfirewall firewall delete rule name="NanopaintHub EdgeWebView2" 2>$null | Out-Null
        netsh advfirewall firewall add rule name="NanopaintHub EdgeWebView2" dir=out action=allow program="$($_.FullName)" enable=yes | ForEach-Object { Log $_ }
    }
}

# 7. Exclusão Defender (pastas build)
$buildDirs = @(
    (Join-Path $RepoRoot "Clients\NanopaintHub.WinUiClient\bin"),
    (Join-Path $RepoRoot "Clients\NanopaintHub.WinUiClient\obj")
)
foreach ($dir in $buildDirs) {
    if (Test-Path $dir) {
        Log "Defender exclusion: $dir"
        Add-MpPreference -ExclusionPath $dir -ErrorAction SilentlyContinue
    }
}

# 8. Diagnóstico
$diagScript = Join-Path $RepoRoot "scripts\diagnose-webview2.ps1"
if (Test-Path $diagScript) {
    Log "A correr diagnose-webview2.ps1..."
    & $diagScript 2>&1 | ForEach-Object { Log $_ }
}

Log "=== Concluído. Relatório: $report ==="
Log "Se WebView2 embebido continuar a falhar, use «Editar documento (Quill)» na app (Chromium/Puppeteer)."
