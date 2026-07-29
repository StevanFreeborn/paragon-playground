<#
.SYNOPSIS
    Starts the development environment via Docker Compose.
.PARAMETER Detached
    Run containers in the background. Defaults to true.
#>
param(
    [switch]$Detached = $true
)

$composeFile = Join-Path $PSScriptRoot '..\docker-compose.dev.yml'

if (-not (Test-Path $composeFile)) {
    Write-Host "Compose file not found at $composeFile" -ForegroundColor Red
    exit 1
}

Write-Host "Starting development environment..." -ForegroundColor Cyan
$detachArg = if ($Detached) { '-d' } else { '' }
docker compose -f $composeFile up --build $detachArg

if ($LASTEXITCODE -eq 0 -and $Detached) {
    Write-Host ""
    Write-Host "All services started. Run 'scripts/dev-down.ps1' to stop." -ForegroundColor Green
    Write-Host "Open https://acme.paragonplayground.localhost in your browser." -ForegroundColor Green
}
exit $LASTEXITCODE
