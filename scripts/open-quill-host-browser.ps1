#Requires -Version 5.1
<#
.SYNOPSIS
  Teste A1: abre quill-host.html no browser (Quill isolado do WebView2 WinUI).
#>
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$hostPath = Join-Path $repoRoot 'Clients\NanopaintHub.WinUiClient\Assets\Editor\quill-host.html'

if (-not (Test-Path $hostPath)) {
    Write-Error "quill-host.html não encontrado: $hostPath"
}

Write-Host "=== Teste A1: Quill no browser ===" -ForegroundColor Cyan
Write-Host "Ficheiro: $hostPath"
Write-Host ""
Write-Host "Verifique manualmente:"
Write-Host "  1. Barra de ferramentas Quill visível"
Write-Host "  2. Rodapé: 'Editor pronto para edição'"
Write-Host "  3. F12 DevTools: sem erros em quill.min.js / quill.snow.css"
Write-Host ""

$uri = [Uri]::new((Resolve-Path $hostPath).Path)
Start-Process $uri.AbsoluteUri
Write-Host "Browser aberto. Registe o resultado em docs/webview2-debug.md (secção Matriz A1)."
