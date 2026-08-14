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

This uses [mkcert](https://github.com/FiloSottile/mkcert) (installed automatically if missing). No hosts file entries needed — `*.paragonplayground.localhost` resolves to 127.0.0.1 natively.

### 2. Start infrastructure

```powershell
cd src\ParagonPlayground
docker compose -f docker-compose.dev.yml up -d
```

### 3. Seed data (via CLI)

```powershell
cd src\ParagonPlayground\backend
dotnet run --project src\ParagonPlayground.Cli -- provision seed
```

Or create your own org + user:

```powershell
dotnet run --project src\ParagonPlayground.Cli -- provision org --name "Test Corp" --slug testcorp
dotnet run --project src\ParagonPlayground.Cli -- provision user --email admin@testcorp.com --password s3cret --name "Admin" --org-slug testcorp
```

### 4. Open the app

Navigate to **https://acme.paragonplayground.localhost** and sign in with:

- **Email:** alice@acme.com
- **Password:** password123

## CLI Reference

```powershell
# Run from the backend directory
dotnet run --project src\ParagonPlayground.Cli -- provision org --name "Name" --slug name
dotnet run --project src\ParagonPlayground.Cli -- provision user --email e@x.com --password p --name "User" --org-slug name
dotnet run --project src\ParagonPlayground.Cli -- provision seed
```
