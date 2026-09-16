# Deployment — Requirements

> **Section:** Deployment  
> **Document:** Requirements  
> **Related:** [Development.md](Development.md) | [Staging.md](Staging.md) | [Production.md](Production.md)

---

## Runtime Requirements

| Requirement | Detail |
|---|---|
| .NET Runtime | .NET 10 (ASP.NET Core hosting) |
| Database | SQL Server (version TBD; SQL Server 2019+ recommended) |
| Web server | Kestrel (via ASP.NET Core) or IIS/Nginx reverse proxy |
| HTTPS | Required in staging and production |
| Browser | Modern browser with WebAssembly support (for Blazor WASM client) |

---

## Build Requirements

| Requirement | Detail |
|---|---|
| .NET SDK | .NET 10 SDK |
| Build tool | `dotnet build` / `dotnet publish` |
| EF Core CLI | `dotnet ef` (for migration management) |
| Source control | Git |

---

## Infrastructure Requirements

| Component | Requirement |
|---|---|
| Application server | Windows Server or Linux with .NET 10 runtime |
| Database server | SQL Server instance (separate from app server recommended for production) |
| TLS certificate | Valid TLS certificate for HTTPS in staging/production |
| Secrets management | Environment-appropriate secret management (see below) |

---

## Configuration and Secret Management Requirements

| Environment | Required Mechanism |
|---|---|
| Development | `appsettings.Development.json` (.NET User Secrets must NOT be used) |
| Staging | `appsettings.Staging.json` |
| Production | `appsettings.Production.json` |

All application configuration must be stored in appsettings files. In development, the local development signing key is maintained in `appsettings.Development.json`. Production credentials and signing keys are maintained in environment-specific appsettings files for deployed environments.

---

## Ports and Networking

| Component | Default Port | Notes |
|---|---|---|
| ASP.NET Core API | 7001 (HTTPS) / 5001 (HTTP dev) | Configurable |
| SQL Server | 1433 | Standard SQL Server port |

---

## Related Documents

| Document | Link |
|---|---|
| Development Setup | [Development.md](Development.md) |
| Staging | [Staging.md](Staging.md) |
| Production | [Production.md](Production.md) |
| Security Guidelines | [../Security/Security-Guidelines.md](../Security/Security-Guidelines.md) |
