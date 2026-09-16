# System Architecture — Architecture

> **Section:** System Architecture  
> **Document:** Architecture Detail  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Overview.md](Overview.md) | [Project-Structure.md](Project-Structure.md)

---

## Architectural Style: Onion Architecture

DO.OneAccess uses **Onion Architecture** (also known as Clean Architecture / Ports and Adapters). This style ensures that the Domain is isolated from infrastructure concerns and external frameworks.

---

## Layer Definitions

### Domain Layer — `DO.OneAccess.Domain`

The innermost layer. Contains:

- **Entities** — Core business objects (e.g., `User`, `Division`, `Section`, `System`).
- **Enumerations** — Domain-level enums (e.g., `RoleType`, `DefaultAccess`, `AccessType`).
- **Domain Interfaces** — Abstractions owned by the domain (e.g., `IRepository<T>`).
- **Domain Rules / Guards** — Business invariants enforced at the domain level.

**Must not depend on:** Infrastructure, Server, Client, or any external framework.

---

### Application Layer — `DO.OneAccess.Application`

Orchestrates domain entities to fulfill use cases. Contains:

- **Service Interfaces** — Contracts for application services (e.g., `IAuthService`, `IUserService`).
- **Use Cases / Commands / Queries** — Application business logic.
- **DTOs** — Data transfer objects for API communication.
- **Validators** — Input validation rules and validator classes.
- **Mapping Profiles** — Entity ↔ DTO mappings.

**Depends on:** Domain only.  
**Must not depend on:** Infrastructure, Server, Client.

---

### Infrastructure Layer — `DO.OneAccess.Infrastructure`

Implements the interfaces defined in Application/Domain. Contains:

- **EF Core DbContext and Configurations** — Entity mappings, table configurations.
- **Repositories** — Concrete data access implementations.
- **Migrations** — EF Core database migrations.
- **Password Hashing** — Concrete password hashing implementation.
- **Token Services** — Refresh token hashing and management.

**Depends on:** Application and Domain.  
**Must not depend on:** Server or Client.

---

### Server Layer — `DO.OneAccess.Server`

The ASP.NET Core Web API host. Contains:

- **Controllers** — HTTP request handling.
- **Middleware** — Exception handling, logging, request pipeline.
- **JWT Configuration** — Token generation and validation setup.
- **Authorization Policies** — Role-based and policy-based authorization definitions.
- **Dependency Injection Registration** — Service wiring.
- **Configuration** — `appsettings.json`, environment-specific files, secrets.

**Depends on:** Application, Infrastructure (for DI wiring).  
**Must not depend on:** Client.

---

### Client Layer — `DO.OneAccess.Client`

The Blazor WebAssembly single-page application. Contains:

- **Blazor Pages and Components** — UI.
- **Client-Side Services** — API call abstractions, authentication state.
- **Client Authorization** — Route guards, UI display rules (not security enforcement).
- **HTTP Client Configuration** — Base address, token attachment.

**Depends on:** Shared DTOs (from Application layer or a shared project).  
**Must not contain:** Server secrets, server-side logic, database access.

---

## Dependency Direction

```
Client ──────────────────────────────────────▶ Server
                                                  │
                                                  ▼
Infrastructure ──────────────────────────▶ Application
                                                  │
                                                  ▼
                                               Domain
```

The Domain has **no outward dependencies**. All arrows point inward toward the Domain.

---

## Key Architectural Decisions

| Decision | Rationale |
|---|---|
| Onion Architecture | Protects domain logic from infrastructure changes; testability |
| Blazor WebAssembly | Single .NET ecosystem for client and server |
| JWT + Refresh Tokens | Stateless API auth with secure session management |
| EF Core with SQL Server | Well-supported ORM for .NET; straightforward migrations |
| No full SSO/OIDC in v1 | Avoid over-engineering; remain integration-ready without unnecessary complexity |
| GUID-based identity | Stable, collision-resistant identifiers for integration-facing entities (`UserId`, `SystemId`), with HR-assigned `EmployeeNumber` for business identity |

---

## Authorization Architecture

- **Server-side is authoritative.** All authorization decisions are enforced by the API.
- **Client-side authorization** controls UI visibility only. It is not a security boundary.
- Role and scope checks are performed in the Application layer services and enforced at the Controller level via `[Authorize]` attributes and authorization policies.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Project Structure | [Project-Structure.md](Project-Structure.md) |
| Security Overview | [../Security/Security-Overview.md](../Security/Security-Overview.md) |
| Database Architecture | [Database-Architecture.md](Database-Architecture.md) |
