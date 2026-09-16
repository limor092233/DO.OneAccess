# DO.OneAccess

**DO.OneAccess** is a centralized online authentication and system-access portal for authorized office employees. Employees use one portal to access the office systems they are authorized to use.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Language | C# |
| Frontend | Blazor WebAssembly |
| API | ASP.NET Core Web API |
| Authentication | JWT (Access + Refresh Tokens) |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Architecture | Onion Architecture |

---

## Solution Structure

```
DO.OneAccess/
├── src/
│   ├── DO.OneAccess.Domain          # Core domain entities and interfaces
│   ├── DO.OneAccess.Application     # Use cases, DTOs, service interfaces
│   ├── DO.OneAccess.Infrastructure  # EF Core, repositories, external services
│   ├── DO.OneAccess.Server          # ASP.NET Core Web API host
│   └── DO.OneAccess.Client          # Blazor WebAssembly frontend
├── tests/
│   ├── DO.OneAccess.UnitTests
│   └── DO.OneAccess.IntegrationTests
└── docs/
    └── (see documentation below)
```

---

## Documentation

### Master Specification
- [DO.OneAccess Specification v1](docs/DO.OneAccess-Specification-v1.md) — Primary source of truth

### System Architecture
- [Overview](docs/System-Architecture/Overview.md)
- [Architecture](docs/System-Architecture/Architecture.md)
- [Project Structure](docs/System-Architecture/Project-Structure.md)
- [Database Architecture](docs/System-Architecture/Database-Architecture.md)
- [ERD](docs/System-Architecture/ERD.md)
- [Integration Architecture](docs/System-Architecture/Integration-Architecture.md)

### Security
- [Security Overview](docs/Security/Security-Overview.md)
- [Authentication](docs/Security/Authentication.md)
- [JWT](docs/Security/JWT.md)
- [Authorization](docs/Security/Authorization.md)
- [Security Guidelines](docs/Security/Security-Guidelines.md)

### Access Control
- [Overview](docs/Access-Control/Overview.md)
- [Roles](docs/Access-Control/Roles.md)
- [Administrator Scope](docs/Access-Control/Administrator-Scope.md)
- [System Access](docs/Access-Control/System-Access.md)
- [Access Rules](docs/Access-Control/Access-Rules.md)

### Frontend
- [Overview](docs/Frontend/Overview.md)
- [Architecture](docs/Frontend/Architecture.md)
- [Project Structure](docs/Frontend/Project-Structure.md)
- [UI Design](docs/Frontend/UI-Design.md)
- [Navigation](docs/Frontend/Navigation.md)
- [Authentication Flow](docs/Frontend/Authentication-Flow.md)
- [Authorization](docs/Frontend/Authorization.md)
- [State Management](docs/Frontend/State-Management.md)
- [API Integration](docs/Frontend/API-Integration.md)
- [Component Guidelines](docs/Frontend/Component-Guidelines.md)

### Backend
- [Overview](docs/Backend/Overview.md)
- [Architecture](docs/Backend/Architecture.md)
- [API Design](docs/Backend/API-Design.md)
- [Application Layer](docs/Backend/Application-Layer.md)
- [Validation](docs/Backend/Validation.md)

### Database
- [Schema](docs/Database/Schema.md)
- [Tables](docs/Database/Tables.md)
- [Relationships](docs/Database/Relationships.md)
- [Indexes](docs/Database/Indexes.md)
- [Migration Strategy](docs/Database/Migration-Strategy.md)
- [Seed Data](docs/Database/Seed-Data.md)

### Deployment
- [Requirements](docs/Deployment/Requirements.md)
- [Development](docs/Deployment/Development.md)
- [Staging](docs/Deployment/Staging.md)
- [Production](docs/Deployment/Production.md)

### Integration
- [Overview](docs/Integration/Overview.md)
- [Integration Contract](docs/Integration/Integration-Contract.md)
- [Identity](docs/Integration/Identity.md)
- [Future SSO](docs/Integration/Future-SSO.md)

### Updates & Change Management
- [Update Process](docs/Updates/Update-Process.md)
- [Database Migrations](docs/Updates/Database-Migrations.md)
- [Release Process](docs/Updates/Release-Process.md)
- [Changelog](docs/Updates/Changelog.md)

---

## Documentation Rule

Any significant change involving Architecture, Security, Authentication, Authorization, Database, Access Control, Frontend architecture, Backend architecture, Deployment, or Integration **must** update the corresponding documentation under `/docs`.

---

*Version: 1.14.0 | Documentation Status: Reconciled against Current Implementation (2026-09-16)*
