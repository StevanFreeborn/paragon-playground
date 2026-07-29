<#
.SYNOPSIS
    Cleans .NET build artifacts (bin/obj) for all projects in the solution.
#>

$repoRoot = Join-Path $PSScriptRoot '..'
$sln = Join-Path $repoRoot 'backend\ParagonPlayground.slnx'

if (-not (Test-Path $sln)) {
    Write-Host "Solution not found at $sln" -ForegroundColor Red
    exit 1
}

Write-Host "Running dotnet clean..." -ForegroundColor Cyan
dotnet clean $sln --verbosity quiet

Write-Host "Removing bin/obj directories..." -ForegroundColor Cyan
$dirs = Get-ChildItem -Path (Join-Path $repoRoot 'backend') -Include bin,obj -Recurse -Directory
$dirs | Remove-Item -Recurse -Force

Write-Host "Cleaned $($dirs.Count) directories." -ForegroundColor Green
exit $LASTEXITCODE
