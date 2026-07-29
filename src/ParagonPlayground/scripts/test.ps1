<#
.SYNOPSIS
    Runs .NET tests for the solution.
.PARAMETER Configuration
    Build configuration (Debug | Release). Defaults to Debug.
.PARAMETER NoBuild
    Skip build step. Defaults to false.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$NoBuild
)

$repoRoot = Join-Path $PSScriptRoot '..'
$sln = Join-Path $repoRoot 'backend\ParagonPlayground.slnx'

if (-not (Test-Path $sln)) {
    Write-Host "Solution not found at $sln" -ForegroundColor Red
    exit 1
}

$noBuildArg = if ($NoBuild) { '--no-build' } else { '' }

Write-Host "Running tests ($Configuration)..." -ForegroundColor Cyan
dotnet test $sln --configuration $Configuration $noBuildArg --verbosity normal
exit $LASTEXITCODE
