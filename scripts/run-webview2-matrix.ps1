#Requires -Version 5.1
<#
.SYNOPSIS
  Matriz de testes WebView2: diagnose + build smoke apps + relatório.
#>
param(
    [switch]$BuildOnly,
    [switch]$SkipDiagnose
)

$ErrorActionPreference = 'Continue'
$repoRoot = Split-Path $PSScriptRoot -Parent
$reportDir = Join-Path $env:LOCALAPPDATA 'NanopaintHub\Logs'
$reportPath = Join-Path $reportDir 'webview2-matrix-report.txt'
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$lines = @("=== WebView2 Matrix Report ===", "Generated: $timestamp", "")

function Add-Line($text) {
    $script:lines += $text
    Write-Host $text
}

if (-not $SkipDiagnose) {
    Add-Line "--- A2: diagnose-webview2.ps1 ---"
    & (Join-Path $PSScriptRoot 'diagnose-webview2.ps1') 2>&1 | ForEach-Object { Add-Line $_ }
    Add-Line ""
}

Add-Line "--- Build smoke projects ---"
$winUiProj = Join-Path $repoRoot 'Tools\WebView2.WinUiSmoke\WebView2.WinUiSmoke.csproj'
$winFormsProj = Join-Path $repoRoot 'Tools\WebView2.WinFormsSmoke\WebView2.WinFormsSmoke.csproj'

foreach ($proj in @($winUiProj, $winFormsProj)) {
    Add-Line "dotnet build $proj"
    $build = dotnet build $proj -c Debug 2>&1
    $build | ForEach-Object { Add-Line $_ }
    if ($LASTEXITCODE -ne 0) {
        Add-Line "BUILD FAILED: $proj"
    }
    Add-Line ""
}

if (-not $BuildOnly) {
    Add-Line "--- A1: Quill no browser ---"
    Add-Line "Execute: scripts\open-quill-host-browser.ps1"
    Add-Line ""

    Add-Line "--- Smoke logs (se já executou as apps) ---"
    @(
        (Join-Path $reportDir 'wv2-smoke-winui.log'),
        (Join-Path $reportDir 'wv2-smoke-winforms.log')
    ) | ForEach-Object {
        if (Test-Path $_) {
            Add-Line ">> $_"
            Get-Content $_ -Tail 15 | ForEach-Object { Add-Line "   $_" }
        } else {
            Add-Line "(log ausente) $_"
            Add-Line "  Execute manualmente:"
            if ($_ -like '*winui*') {
                Add-Line "  dotnet run --project Tools\WebView2.WinUiSmoke\WebView2.WinUiSmoke.csproj -c Debug"
            } else {
                Add-Line "  dotnet run --project Tools\WebView2.WinFormsSmoke\WebView2.WinFormsSmoke.csproj -c Debug"
            }
        }
        Add-Line ""
    }
}

Add-Line "--- Matriz de decisão ---"
Add-Line "| Teste | OK | Falha |"
Add-Line "| Quill Edge (A1) | Quill OK | Corrigir Assets/Editor |"
Add-Line "| WinForms smoke (A4) | Runtime/host OK | PC/política/AV |"
Add-Line "| WinUI smoke (A3) | WinUI OK | WinUI composition |"
Add-Line "| WinForms OK + WinUI falha | Bug WinUI específico |"
Add-Line "| NanopaintHub falha + smokes OK | Layout/pipeline NanopaintHub |"

$lines | Set-Content -Path $reportPath -Encoding UTF8
Add-Line ""
Add-Line "Relatório: $reportPath"
