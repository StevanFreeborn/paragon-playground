# ParagonPlayground — Getting Started

A application harness to explore using Paragon for integrations.

## Prerequisites

- Docker Desktop
- .NET 10 SDK
- Node.js 24+

## Setup

### 1. Generate TLS certificates

```powershell
# Run once to create + trust a local CA and sign a wildcard cert
.\scripts\setup-certs.ps1
```

> **Run this from an elevated (Admin) PowerShell prompt.** `mkcert` needs admin rights to install the CA into the JDK system trust store. Without elevation the OS and browsers still trust the cert, but Java tools will reject it.

This uses [mkcert](https://github.com/FiloSottile/mkcert) (installed automatically if missing). No hosts file entries needed — `*.paragonplayground.localhost` resolves to 127.0.0.1 natively.

### 2. Start infrastructure

```bash
cd src\ParagonPlayground
docker compose -f docker-compose.dev.yml up -d
```

### 3. Seed data (via CLI)

```bash
cd src\ParagonPlayground\backend
dotnet run --project src\ParagonPlayground.Cli -- provision seed
```

Or create your own org + user:

```bash
dotnet run --project src\ParagonPlayground.Cli -- provision org --name "Test Corp" --slug testcorp
dotnet run --project src\ParagonPlayground.Cli -- provision user --email admin@testcorp.com --password s3cret --name "Admin" --org-slug testcorp
```

### 4. Open the app

Navigate to **https://acme.paragonplayground.localhost** and sign in with:

- **Email:** alice@acme.com
- **Password:** password123

## CLI Reference

```bash
# Run from the backend directory
dotnet run --project src\ParagonPlayground.Cli -- provision org --name "Name" --slug name
dotnet run --project src\ParagonPlayground.Cli -- provision user --email e@x.com --password p --name "User" --org-slug name
dotnet run --project src\ParagonPlayground.Cli -- provision seed
```
