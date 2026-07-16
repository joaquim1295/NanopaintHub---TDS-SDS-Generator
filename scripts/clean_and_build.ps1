# Clean and Build Script for NanopaintHub
# This script removes bin and obj directories recursively and rebuilds the solution.

Write-Host "Cleaning bin and obj directories..." -ForegroundColor Cyan

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
# Remove bin and obj folders
Get-ChildItem -Path $projectRoot -Recurse -Include "bin","obj" -Directory | ForEach-Object {
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue -Path $_.FullName
    Write-Host "Removed $($_.FullName)" -ForegroundColor DarkGray
}

Write-Host "Rebuilding solution..." -ForegroundColor Cyan

# Run dotnet build
$solutionPath = Join-Path $projectRoot "NanopaintHub.sln"
$buildResult = dotnet build $solutionPath -c Debug
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded." -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE." -ForegroundColor Red
}
