# Backend — Architecture

> **Section:** Backend  
> **Document:** Backend Architecture  
> **Related:** [Overview.md](Overview.md) | [Application-Layer.md](Application-Layer.md) | [../System-Architecture/Architecture.md](../System-Architecture/Architecture.md)

---

## Layered Architecture (Server-Side)

```
┌───────────────────────────────────────────────┐
│         DO.OneAccess.Server (API Host)         │
│  Controllers │ Middleware │ DI │ JWT Config    │
└──────────────────────┬────────────────────────┘
                       │ calls via interfaces
                       ▼
┌───────────────────────────────────────────────┐
│       DO.OneAccess.Application                │
│  Services │ Use Cases │ DTOs │ Validators     │
└──────────────────────┬────────────────────────┘
                       │ calls via interfaces
                       ▼
┌───────────────────────────────────────────────┐
│       DO.OneAccess.Domain                     │
│  Entities │ Enums │ Domain Interfaces         │
└───────────────────────────────────────────────┘
              ▲
              │ implements
┌─────────────┴─────────────────────────────────┐
│       DO.OneAccess.Infrastructure             │
│  EF Core │ Repositories │ Hashing            │
└───────────────────────────────────────────────┘
```

---

## Request Pipeline

```
HTTP Request
  → HTTPS Redirection
  → Authentication Middleware (JWT validation)
  → Authorization Middleware
  → Exception Handling Middleware
  → Routing
  → [Authorize] enforcement
  → Controller Action
  → Application Service (use case)
  → Domain + Infrastructure
  → HTTP Response
```

---

## Controller Design

- Controllers are thin. They handle HTTP concerns only (request/response, status codes, routing).
- No business logic in controllers.
- Controllers call Application layer services and return results.
- Controllers use `[Authorize]` and `[Authorize(Roles = "...")]` for coarse-grained access control.

---

## Application Service Pattern

Application services implement business use cases:

```
IUserService
  ├── GetUserByIdAsync(Guid userId) → UserDto
  ├── CreateUserAsync(CreateUserDto dto, Guid actorUserId) → UserDto
  ├── UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid actorUserId) → UserDto
  └── DeactivateUserAsync(Guid userId, Guid actorUserId) → void
```

Service methods:
- Accept DTOs as input.
- Validate input (via validators).
- Enforce scope (e.g., verify the actor's Division contains the target user).
- Perform the operation via repositories.
- Return DTOs.
- Write to `AuditLogs` for auditable actions.

---

## Repository Pattern

Repositories abstract data access from the Application layer:

```
IUserRepository
  ├── GetByIdAsync(Guid userId) → User?
  ├── GetByUsernameAsync(string username) → User?
  ├── GetByEmployeeNumberAsync(string employeeNumber) → User?
  ├── AddAsync(User user) → void
  ├── UpdateAsync(User user) → void
  └── ...
```

Repositories work with domain entities. The Application layer maps entities ↔ DTOs.

---

## Exception and Error Handling

- A global exception handling middleware catches unhandled exceptions.
- Exceptions are logged internally (without sensitive data).
- Generic error responses are returned to the client.
- Domain/Application-layer exceptions (e.g., `NotFoundException`, `ForbiddenException`, `ValidationException`) are mapped to appropriate HTTP status codes.

| Exception Type | HTTP Status |
|---|---|
| `NotFoundException` | 404 Not Found |
| `ForbiddenException` | 403 Forbidden |
| `ValidationException` | 400 Bad Request |
| `UnauthorizedException` | 401 Unauthorized |
| Unhandled | 500 Internal Server Error (generic message) |

---

## Dependency Injection

All services, repositories, and other dependencies are registered in:
- `DO.OneAccess.Infrastructure` — `IServiceCollection` extension method.
- `DO.OneAccess.Server` — `Program.cs` wires everything together.

---

## Configuration

All application configuration must be stored in `appsettings` files:
- `appsettings.json` — Shared defaults.
- `appsettings.Development.json` — Development overrides (including development `Jwt:Key`). .NET User Secrets must NOT be used.
- `appsettings.Production.json` / `appsettings.Staging.json` — Environment-specific appsettings for deployed environments.

---

## Related Documents

| Document | Link |
|---|---|
| Backend Overview | [Overview.md](Overview.md) |
| Application Layer | [Application-Layer.md](Application-Layer.md) |
| API Design | [API-Design.md](API-Design.md) |
| Validation | [Validation.md](Validation.md) |
| System Architecture | [../System-Architecture/Architecture.md](../System-Architecture/Architecture.md) |
| Security | [../Security/Security-Overview.md](../Security/Security-Overview.md) |
