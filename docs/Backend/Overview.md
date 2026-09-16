# Backend — Overview

> **Section:** Backend  
> **Document:** Overview  
> **Related:** [Architecture.md](Architecture.md) | [API-Design.md](API-Design.md) | [Application-Layer.md](Application-Layer.md)

---

## Technology

The DO.OneAccess backend consists of two server-side projects:

| Project | Role |
|---|---|
| `DO.OneAccess.Server` | ASP.NET Core Web API host; HTTP, JWT, middleware, controllers |
| `DO.OneAccess.Infrastructure` | EF Core, repositories, migrations, password/token hashing |

These projects implement the contracts defined in:

| Project | Role |
|---|---|
| `DO.OneAccess.Application` | Use cases, service interfaces, DTOs, validators |
| `DO.OneAccess.Domain` | Entities, enums, domain interfaces |

---

## Responsibilities

### `DO.OneAccess.Server`
- Expose RESTful API endpoints.
- Issue and validate JWT access tokens.
- Enforce route-level authorization via `[Authorize]` attributes.
- Configure middleware (exception handling, HTTPS redirection, CORS).
- Wire dependencies via DI.
- Serve the Blazor WebAssembly client (as static files or via a separate host).

### `DO.OneAccess.Infrastructure`
- Implement the `AppDbContext` using EF Core.
- Implement repository interfaces defined in Application/Domain.
- Implement password hashing and refresh token hashing.
- Manage database migrations.

### `DO.OneAccess.Application`
- Implement use-case services (e.g., `AuthService`, `UserService`, `SystemAccessService`).
- Validate input DTOs.
- Enforce business rules.
- Enforce scope rules (e.g., Administrator scope validation).
- Map between domain entities and DTOs.

### `DO.OneAccess.Domain`
- Define all domain entities.
- Define enumerations.
- Define repository/service interfaces (owned by the domain).

---

## Authorization Enforcement

All authorization is enforced server-side. See [../Security/Authorization.md](../Security/Authorization.md).

---

## Related Documents

| Document | Link |
|---|---|
| Architecture | [Architecture.md](Architecture.md) |
| API Design | [API-Design.md](API-Design.md) |
| Application Layer | [Application-Layer.md](Application-Layer.md) |
| Validation | [Validation.md](Validation.md) |
| Security Overview | [../Security/Security-Overview.md](../Security/Security-Overview.md) |
