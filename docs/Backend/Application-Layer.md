# Backend — Application Layer

> **Section:** Backend  
> **Document:** Application Layer  
> **Related:** [Architecture.md](Architecture.md) | [Validation.md](Validation.md)

---

## Responsibility

The Application layer (`DO.OneAccess.Application`) is the home of use-case logic. It:

- Implements business use cases as service classes.
- Defines service interfaces (contracts).
- Defines DTOs for request/response data.
- Validates inputs.
- Enforces scope rules (e.g., Administrator Division scope).
- Writes to `AuditLogs` for auditable actions.
- Maps between domain entities and DTOs.
- Has no knowledge of HTTP, EF Core, or the UI.

---

## Service Interface Pattern

Service interfaces are defined in `Application/Common/Interfaces/`:

```
IAuthService        — login, logout, refresh token, initial setup
IUserService        — user CRUD, role management, sysadmin role transfer
IDivisionService    — division CRUD, scoped division retrieval
ISectionService     — section CRUD
ISystemService      — system registration and management
ISystemAccessService — access resolution, UserSystemAccess management
IAdminScopeService  — administrator scope assignment
IAuditService       — audit log writing and retrieval
ILoginHistoryService — login history recording and retrieval
```

---

## Use Case Pattern (Service Methods)

Each service method represents a distinct use case:

```
IUserService
  ├── GetUserByIdAsync(Guid userId, Guid actorUserId) → UserDto
  ├── GetUsersAsync(Guid actorUserId, UserQueryDto query) → PagedResult<UserDto>
  ├── CreateUserAsync(CreateUserDto dto, Guid actorUserId) → UserDto
  ├── TransferSystemAdministratorAsync(TransferSystemAdministratorDto dto, Guid actorUserId) → void
  ├── UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid actorUserId) → UserDto
  ├── DeactivateUserAsync(Guid userId, Guid actorUserId) → void
  └── ChangeRoleAsync(Guid userId, short roleId, Guid actorUserId) → void
```

The `actorUserId` parameter is the authenticated user performing the action. It is used for:
- Scope validation (is the actor authorized to act on the target?).
- Audit log entries.

---

## Scope Enforcement Pattern

For Administrator-scoped operations:

```
1. Resolve actor's role from JWT claims.
2. If actor is SystemAdministrator → skip scope check.
3. If actor is Administrator:
   a. Load AdministratorScope for actor's UserId.
   b. Resolve target entity's Division.
   c. If actor.DivisionId ≠ target.DivisionId → throw ForbiddenException.
4. Proceed with the operation.
```

This pattern is applied consistently in all Administrator-accessible service methods.

---

## Audit Logging Pattern

Auditable actions write to `AuditLogs` via `IAuditService`:

```csharp
await _auditService.LogAsync(new AuditLogDto
{
    UserId = actorUserId,
    Action = "CreateUser",
    EntityName = "Users",
    EntityId = newUser.UserId.ToString(),
    OldValues = null,
    NewValues = JsonSerializer.Serialize(new { newUser.UserId, newUser.Username, newUser.RoleId }),
    IpAddress = ipAddress,
    UserAgent = userAgent,
    CreatedAt = DateTime.UtcNow
});
```

> Audit log payloads must not contain passwords, tokens, or secrets.

---

## DTO Design

DTOs are used to decouple the API surface from the domain model:

| DTO Type | Purpose |
|---|---|
| `CreateXxxDto` | Input for creating a resource |
| `UpdateXxxDto` | Input for updating a resource |
| `XxxDto` | Output/response representation |
| `XxxQueryDto` | Filter/pagination parameters for list queries |
| `PagedResult<T>` | Paginated list response |

DTOs live in `Application/DTOs/`.

---

## Mapping

Mapping between domain entities and DTOs may use:
- **AutoMapper** — widely used; configuration-driven.
- **Manual mapping** — explicit and type-safe; simpler for smaller surfaces.

The strategy is to be decided during implementation. Document the decision when made.

---

## Application Exceptions

Custom exception types in `Application/Common/Exceptions/`:

| Exception | Meaning | Maps to HTTP |
|---|---|---|
| `NotFoundException` | Requested entity does not exist | 404 |
| `ForbiddenException` | Actor lacks scope or role for action | 403 |
| `ValidationException` | Input failed validation | 400 |
| `ConflictException` | Unique constraint or business rule conflict | 409 |

---

## Related Documents

| Document | Link |
|---|---|
| Backend Architecture | [Architecture.md](Architecture.md) |
| Validation | [Validation.md](Validation.md) |
| API Design | [API-Design.md](API-Design.md) |
| Access Rules | [../Access-Control/Access-Rules.md](../Access-Control/Access-Rules.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
