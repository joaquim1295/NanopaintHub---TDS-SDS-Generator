#Requires -Version 5.1
<#
.SYNOPSIS
  Diagnostico WebView2 no PC (runtime vs problema WinUI).
#>
$ErrorActionPreference = 'Continue'
Write-Host '=== NanopaintHub WebView2 Diagnostics ===' -ForegroundColor Cyan

$guid = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}'
$reg = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\$guid" -ErrorAction SilentlyContinue
Write-Host ''
Write-Host '[Registry Evergreen]'
Write-Host "  pv:       $($reg.pv)"
Write-Host "  location: $($reg.location)"

$roots = @(
    "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application",
    "${env:ProgramFiles}\Microsoft\EdgeWebView\Application"
)
Write-Host ''
Write-Host '[Browser folders]'
foreach ($root in $roots) {
    if (Test-Path $root) {
        Get-ChildItem $root -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            $exe = Join-Path $_.FullName 'msedgewebview2.exe'
            Write-Host "  $($_.FullName) -> exe=$(Test-Path $exe)"
        }
    }
    else {
        Write-Host "  (missing) $root"
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$binCandidates = @(
    (Join-Path $repoRoot 'Clients\NanopaintHub.WinUiClient\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64'),
    (Join-Path $repoRoot 'Clients\NanopaintHub.WinUiClient\bin\Debug\net9.0-windows10.0.26100.0\win-x64')
)
Write-Host ''
Write-Host '[App output]'
foreach ($bin in $binCandidates) {
    if (-not (Test-Path $bin)) { continue }
    Write-Host "  folder: $bin"
    @('WebView2Loader.dll', 'Microsoft.Web.WebView2.Core.dll') | ForEach-Object {
        $p = Join-Path $bin $_
        Write-Host "    $_ : $(Test-Path $p)"
    }
    break
}

Write-Host ''
Write-Host '[User profiles]'
$profiles = Join-Path $env:LOCALAPPDATA 'NanopaintHub\WebView2'
if (Test-Path $profiles) {
    Get-ChildItem $profiles -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "  $($_.Name)" }
}
else {
    Write-Host '  (none)'
}

Write-Host ''
Write-Host '[Running msedgewebview2 processes]'
$wv2 = Get-Process -Name msedgewebview2 -ErrorAction SilentlyContinue
if ($wv2) {
    Write-Host "  count: $($wv2.Count)"
    $wv2 | Select-Object -First 5 | ForEach-Object { Write-Host "  pid=$($_.Id)" }
    if ($wv2.Count -gt 10) {
        Write-Host '  AVISO: muitos processos WebView2 - feche apps e reinicie.' -ForegroundColor Yellow
    }
}
else {
    Write-Host '  (none)'
}

Write-Host ''
Write-Host '[Interpretation]'
Write-Host '  - Runtime OK + msedgewebview2.exe existe => Evergreen instalado (MAQUINA OK).'
Write-Host '  - MÁQUINA OK na app mas Ensure falha => WinUI/HWND ou AV a bloquear subprocesso desta app.'
Write-Host '  - msedgewebview2Count nao aumenta durante Ensure => subprocesso bloqueado.'
Write-Host '  - Teste: exclusao Defender para NanopaintHub.WinUIClient.exe.'
Write-Host '  - Logs: %LocalAppData%\NanopaintHub\Logs\webview2-trace.log'

Write-Host ''
Write-Host 'Done.'
