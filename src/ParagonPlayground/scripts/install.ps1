<#
.SYNOPSIS
    Restores .NET packages and installs frontend npm dependencies.
#>

$projectRoot = Join-Path $PSScriptRoot '..'
$backendDir = Join-Path $projectRoot 'backend'
$frontendDir = Join-Path $projectRoot 'frontend'
$sln = Join-Path $backendDir 'ParagonPlayground.slnx'

Write-Host "Restoring .NET packages..." -ForegroundColor Cyan
if (Test-Path $sln) {
    dotnet restore $sln
} else {
    dotnet restore $backendDir
}

Write-Host "`nInstalling frontend npm dependencies..." -ForegroundColor Cyan
if (Test-Path $frontendDir) {
    Push-Location $frontendDir
    npm install
    Pop-Location
}

Write-Host "`nAll dependencies installed." -ForegroundColor Green
exit $LASTEXITCODE
