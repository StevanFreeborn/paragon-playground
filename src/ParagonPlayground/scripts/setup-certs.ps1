<#
.SYNOPSIS
    Generates local TLS certificates for *.paragonplayground.localhost using mkcert.

.DESCRIPTION
    Uses mkcert (https://github.com/FiloSottile/mkcert) to create a local
    Certificate Authority and sign a wildcard certificate for local development.
    The CA is automatically trusted by the OS and browsers (no security warnings).

    If mkcert is not installed, the script will offer to install it via winget.

.NOTES
    Run this script from an elevated (Admin) PowerShell prompt so mkcert can
    install the CA into the JDK system trust store (cacerts). Without elevation
    the CA is still trusted by the OS and browsers, but Java tools may reject
    the certificate.
#>

$certsDir = Join-Path $PSScriptRoot "..\certs"

if (-not (Test-Path $certsDir)) {
    New-Item -ItemType Directory -Path $certsDir -Force | Out-Null
}

# Check if mkcert is available
$mkcert = Get-Command mkcert -ErrorAction SilentlyContinue

if (-not $mkcert) {
    Write-Host "mkcert not found." -ForegroundColor Yellow
    $install = Read-Host "Install mkcert via winget? (y/n)"
    
    if ($install -eq "y") {
        winget install FiloSottile.mkcert
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Installation failed. Install manually: https://github.com/FiloSottile/mkcert" -ForegroundColor Red
            exit 1
        }

        $env:Path = [Environment]::GetEnvironmentVariable("Path", "User") + ";$env:Path"
    } else {
        Write-Host "Install mkcert manually from https://github.com/FiloSottile/mkcert and re-run." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Installing local CA (may prompt for admin)..." -ForegroundColor Cyan & mkcert -install

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to install CA." -ForegroundColor Red
    exit 1
}

Write-Host "Generating certificate for *.paragonplayground.localhost..." -ForegroundColor Cyan & mkcert -cert-file "$certsDir/_wildcard.paragonplayground.localhost.pem" `
  -key-file "$certsDir/_wildcard.paragonplayground.localhost-key.pem" `
  "*.paragonplayground.localhost"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Certificates generated in $certsDir" -ForegroundColor Green
    Write-Host "  cert: $certsDir\_wildcard.paragonplayground.localhost.pem" -ForegroundColor Green
    Write-Host "  key:  $certsDir\_wildcard.paragonplayground.localhost-key.pem" -ForegroundColor Green
} else {
    Write-Host "Certificate generation failed." -ForegroundColor Red
    exit 1
}
