<#
.SYNOPSIS
    Stops the development environment and removes containers/networks.
#>

$composeFile = Join-Path $PSScriptRoot '..\docker-compose.dev.yml'

if (-not (Test-Path $composeFile)) {
    Write-Host "Compose file not found at $composeFile" -ForegroundColor Red
    exit 1
}

Write-Host "Stopping development environment..." -ForegroundColor Cyan
docker compose -f $composeFile down
exit $LASTEXITCODE
