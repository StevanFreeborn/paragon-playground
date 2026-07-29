<#
.SYNOPSIS
    Builds the .NET solution.
.PARAMETER Configuration
    Build configuration (Debug | Release). Defaults to Debug.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$repoRoot = Join-Path $PSScriptRoot '..'
$sln = Join-Path $repoRoot 'backend\ParagonPlayground.slnx'

if (-not (Test-Path $sln)) {
    Write-Host "Solution not found at $sln" -ForegroundColor Red
    exit 1
}

Write-Host "Building $Configuration..." -ForegroundColor Cyan
dotnet build $sln --configuration $Configuration
exit $LASTEXITCODE
