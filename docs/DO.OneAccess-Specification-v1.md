# DO.OneAccess — Master Specification v1

> **Status:** Foundation Verification Complete / Release Ready for Approved Scope  
> **Version:** 1.2  
> **Date:** 2026-09-11  
> **Classification:** Internal Technical Document

---

## 1. Purpose

DO.OneAccess is a centralized online authentication and system-access portal for authorized office employees. Employees use a single portal to log in and access the office systems they are authorized to use.

---

## 2. Goals

1. Provide a single, secure entry point for employee authentication.
2. Centralize system-access grants and restrictions per user.
3. Provide administrators with scoped management authority over their organizational unit.
4. Maintain a full audit trail of access and administrative actions.
5. Remain integration-ready for future office systems that will reference DO.OneAccess identity.

---

## 3. Non-Goals (v1)

- Full SSO / OIDC / OAuth server — NOT IMPLEMENTED in current release. Future federation may build upon stable identity/system contracts, but any federation design requires a separately approved authentication/federation design.
- Multi-tenancy across separate organizations.
- Self-service registration by users.
- Custom permission tables beyond the approved schema.
- Initial System Administrator Setup UI (deferred backend dependency).
- Real Browser E2E Automation (PENDING — Environment Prerequisites Required).

---

## 4. Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Language | C# |
| Frontend | Blazor WebAssembly |
| API | ASP.NET Core Web API |
| Authentication | JWT (short-lived access token; refresh token in HttpOnly Secure cookie; hashed in DB) |
| ORM | Entity Framework Core |
| Database | SQL Server |
| Architecture | Onion Architecture |

---

## 5. Solution Structure

```
src/
  DO.OneAccess.Domain          → Entities, domain interfaces, domain rules
  DO.OneAccess.Application     → Use cases, service interfaces, DTOs, validators
  DO.OneAccess.Infrastructure  → EF Core DbContext, repositories, migrations
  DO.OneAccess.Server          → ASP.NET Core API, controllers, middleware, JWT
  DO.OneAccess.Client          → Blazor WebAssembly SPA

tests/
  DO.OneAccess.UnitTests
  DO.OneAccess.IntegrationTests
```

**Dependency rule:** Client → Server → Application → Domain. Infrastructure → Application/Domain. Domain has no outward dependencies.

---

## 6. Roles

Exactly three roles exist at launch:

| RoleId (`smallint`) | Code | Name | Description | IsActive |
|---|---|---|---|---|
| 1 | `SYSTEM_ADMINISTRATOR` | System Administrator | Global authority over the entire DO.OneAccess platform | true |
| 2 | `ADMINISTRATOR` | Administrator | Scoped authority over one Division and its Sections | true |
| 3 | `USER` | User | Regular employee account; accesses permitted systems | true |

- Each user has exactly **one** role (`Users.RoleId`).
- **Semantic Rule:** `Code` and `Name` are distinct and must not be treated as interchangeable (`Code` is the programmatic identifier; `Name` is the human-readable display label).

---

## 7. Organizational Hierarchy

```
Division
  └── Section
        └── User (employee account)
```

- A Division contains one or more Sections.
- A Section belongs to exactly one Division.
- Approved Business Rule: **ONE EMPLOYEE = ONE DO.OneAccess USER**.
- The `Employee` entity is consolidated directly into `User`.
- `UserId` is the single stable technical identity across the platform.
- `EmployeeNumber` remains the official unique business identifier.
- `Users.SectionId` is nullable at database level:
  - `SYSTEM_ADMINISTRATOR`: `SectionId = NULL` (global scope).
  - `ADMINISTRATOR`: `SectionId` may be `NULL` (scope derives strictly from `AdministratorScopes.DivisionId`).
  - `USER`: `SectionId` is required (belongs to a Section under a Division).
- **Section Uniqueness:** Section `Code` is unique within its parent Division: `UNIQUE(DivisionId, Code)`. Section `Code` is not required to be globally unique.

---

## 8. Identity & Identifier Strategy

### Canonical Identifier Strategy

1. **Externally Meaningful & Integration-Facing Identities (`uniqueidentifier` / GUID):**
   - `UserId` — stable DO.OneAccess user account identity; referenced by integrations
   - `SystemId` — stable registered system identity; referenced by integrations

2. **Internal Reference Entities (Numeric):**
   - `RoleId` — `smallint`
   - `DivisionId` — `int`
   - `SectionId` — `int`

3. **High-Volume Transactional / History / Access Records (`bigint`):**
   - `AdministratorScopeId` — `bigint`
   - `AdministratorSystemAccessId` — `bigint`
   - `UserSystemAccessId` — `bigint`
   - `RefreshTokenId` — `bigint`
   - `LoginHistoryId` — `bigint`
   - `AuditLogId` — `bigint`
   - `SystemSettingId` — `bigint`

This replaces any older documentation statement that says all primary keys must be GUID.

### Primary Key Naming Rule

Use explicit, entity-specific primary key names throughout the database and documentation:
- `RoleId`, `DivisionId`, `SectionId`, `UserId`, `SystemId`, `SystemSettingId`, `UserSystemAccessId`, `AdministratorScopeId`, `AdministratorSystemAccessId`, `RefreshTokenId`, `LoginHistoryId`, `AuditLogId`.
- Do not use generic `Id` as the canonical database column name.

### User Identity & Login Fields

- `Username` is the unique login identifier (`UNIQUE`).
- `EmployeeNumber` is the official HR-determined employee/business identifier (`UNIQUE`).
- `UserId` (GUID) is the internal platform and integration identifier, distinct from the HR-assigned `EmployeeNumber`.
- `Email` is employee contact/identity information.
- Do not use `EmployeeNumber` or `Email` as the `Users` primary key.

> **Integration Rule:** Future office systems must reference `UserId` and `SystemId`. Do not use username or email as primary integration keys.

---

## 9. System Access Model

Each registered System has a `DefaultAccess` policy:

| Value | Meaning |
|---|---|
| `All` | Accessible to all users by default |
| `Restricted` | Not accessible unless explicitly allowed |

`UserSystemAccess` records an explicit per-user override:

- `UserSystemAccessId` (`bigint` PK)
- `UserId` (`uniqueidentifier` FK)
- `SystemId` (`uniqueidentifier` FK)
- `AccessType` (`nvarchar(20)`) — `Allow` or `Deny`
- `CreatedAt` (`datetime2`)
- `CreatedBy` (`uniqueidentifier` nullable)
- `UpdatedAt` (`datetime2` nullable)
- `UpdatedBy` (`uniqueidentifier` nullable)

**Constraint:** `UNIQUE(UserId, SystemId)`

**Access resolution order:**
1. Explicit `UserSystemAccess` overrides `System.DefaultAccess`.
2. If no explicit rule exists, use `System.DefaultAccess`.

> **Critical distinction:** User system **usage** access and Administrator system **management** scope are entirely separate concepts. See [Access Control](Access-Control/Overview.md).

---

## 10. Administrator Scope

- Each Administrator is assigned exactly one Division via `AdministratorScopes` (`AdministratorScopeId`, `AdministratorUserId`, `DivisionId`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- The Administrator may manage all Sections and Users within that Division.
- The Administrator may manage only Systems explicitly granted to them via `AdministratorSystemAccess` (`AdministratorSystemAccessId`, `AdministratorUserId`, `SystemId`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- Constraint: `UNIQUE(AdministratorUserId, SystemId)`.
- Administrator system-management grants are modeled separately from `UserSystemAccess`.

---

## 11. System Administrator

- **Initial Setup / Bootstrap:** The initial System Administrator is provisioned through a dedicated, secure one-time bootstrap gateway (`POST /api/setup` and client wizard `/setup`). The endpoint is guarded by a one-time cryptographic bootstrap token (ephemeral 256-bit hex token generated and logged to console on development startup; `ONEACCESS_BOOTSTRAP_TOKEN` secret required in production with fail-closed behavior if missing). State detection is dynamic via `Users.AnyAsync(u => u.RoleId == 1)` (no dedicated initialization table). Setup operations (User creation with `RoleId = 1`, `SectionId = null` for universal organizational scope, `EmployeeNumber`, and official profile fields, and audit log emission with `UserId = null` and action `"SYSTEM_BOOTSTRAP"`) execute in a single atomic database transaction (`IsolationLevel.Serializable`). Once any SysAdmin record exists (active or inactive), the setup gateway is permanently locked and returns 409 Conflict. The endpoint is rate-limited (429 Too Many Requests). Passwords, hashes, and bootstrap tokens are strictly excluded from audit logs and client state.
- **Exactly-One System Administrator Invariant:** The application guarantees the exactly-one System Administrator invariant for supported application workflows by restricting SysAdmin assignment to First Run Setup and the atomic Role Transfer workflow, protected by a Serializable transaction and post-condition validation. Additional System Administrators cannot be created, self-registered, or assigned through general role management.
- **Atomic Role Transfer Workflow:** Universal platform authority can only be transferred by the current active System Administrator to an active target User or Administrator via `POST /api/users/system-administrator/transfer`. The target account receives `RoleId = 1` and `SectionId = null`, and any previous Administrator scopes or system grants are cleared. The departing System Administrator must transition to a valid `ADMINISTRATOR` (assigned Division required) or `USER` (assigned Section within selected Division required). All refresh tokens for both accounts are revoked to enforce re-authentication. The operation executes atomically under `IsolationLevel.Serializable` and asserts that `COUNT(Users WHERE RoleId = 1) == 1` before commit. Historical audit logs and login histories are preserved intact.

---

## 12. Security Baseline

| Requirement | Rule |
|---|---|
| Passwords | Never stored in plaintext; use secure hashing (e.g., bcrypt/Argon2) |
| JWT access tokens | Short-lived; stored in client memory only |
| Refresh tokens | Delivered via Secure, HttpOnly, SameSite cookie; never in localStorage or response body; stored server-side as hashes |
| CSRF protection | Required for cookie-based auth endpoints; primary: `SameSite=Strict` |
| Secrets | Never committed to source control |
| Client configuration | Must never contain server secrets |
| Authorization | Enforced server-side by the API; UI hiding is not security |
| Audit logs | Must never contain passwords, tokens, or secrets |
| Logout semantics | `POST /api/auth/logout` allows unauthenticated requests (`[AllowAnonymous]`) because an expired Bearer token must not prevent a client from clearing its session and revoking its refresh token. Security is enforced via the `X-CSRF-TOKEN` header and HttpOnly refresh cookie validation. |

---

## 13. Configuration

- All application configuration must be stored in `appsettings` files (`appsettings.json` and environment-specific `appsettings.{Environment}.json`).
- In development, `Jwt:Key` must be stored in `appsettings.Development.json`. .NET User Secrets must NOT be used for application configuration.
- In production and staging, application configuration is stored in environment-specific appsettings files (e.g., `appsettings.Production.json`).
- `SystemSettings` is for system-specific business configuration only (`SystemSettingId`, `SystemId`, `SettingKey`, `SettingValue`, `IsSecret`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- Do not create a generic `AppSettings` database table for application/environment configuration.

---

## 14. Database Schema (12 Canonical Tables)

| # | Table | Primary Key | Key Fields / Constraints | Purpose |
|---|---|---|---|---|
| 1 | `Roles` | `RoleId` (`smallint`) | `Code` (UK), `Name`, `Description`, `IsActive` | Role definitions |
| 2 | `Divisions` | `DivisionId` (`int`) | `Code` (UK), `Name`, `Description`, `IsActive`, Audit fields | Organizational divisions |
| 3 | `Sections` | `SectionId` (`int`) | `DivisionId` (FK), `Code`, `Name`, `Description`, `IsActive`, `UNIQUE(DivisionId, Code)`, Audit fields | Sections within a division |
| 4 | `Users` | `UserId` (`uniqueidentifier`) | `EmployeeNumber` (UK), `FirstName`, `MiddleName` (null), `LastName`, `Email`, `Position` (null), `SectionId` (FK, null), `RoleId` (FK), `Username` (UK), `PasswordHash`, `IsActive`, `LastLoginAt`, Audit fields | Consolidated employee profile & user account |
| 5 | `Systems` | `SystemId` (`uniqueidentifier`) | `SystemCode` (UK), `SystemName`, `Description`, `BaseUrl`, `IconUrl`, `DefaultAccess`, `IsActive`, Audit fields | Registered office systems |
| 6 | `SystemSettings` | `SystemSettingId` (`bigint`) | `SystemId` (FK), `SettingKey`, `SettingValue`, `IsSecret`, `UNIQUE(SystemId, SettingKey)`, Audit fields | Per-system business configuration |
| 7 | `UserSystemAccess` | `UserSystemAccessId` (`bigint`) | `UserId` (FK), `SystemId` (FK), `AccessType`, `UNIQUE(UserId, SystemId)`, Audit fields | Explicit user/system access overrides |
| 8 | `AdministratorScopes` | `AdministratorScopeId` (`bigint`) | `AdministratorUserId` (FK, UK), `DivisionId` (FK), Audit fields | Administrator division scope grants |
| 9 | `AdministratorSystemAccess` | `AdministratorSystemAccessId` (`bigint`) | `AdministratorUserId` (FK), `SystemId` (FK), `UNIQUE(AdministratorUserId, SystemId)`, Audit fields | Administrator system-management grants |
| 10 | `RefreshTokens` | `RefreshTokenId` (`bigint`) | `UserId` (FK), `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenId`, `CreatedByIp`, `RevokedByIp` | Hashed refresh token store |
| 11 | `LoginHistories` | `LoginHistoryId` (`bigint`) | `UserId` (FK, nullable), `UsernameAttempted`, `Success`, `IpAddress`, `UserAgent`, `FailureReason`, `CreatedAt` | Login event records |
| 12 | `AuditLogs` | `AuditLogId` (`bigint`) | `UserId` (FK, nullable), `Action`, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `CreatedAt` | Administrative and access audit trail |

Full schema details: [Database/Tables.md](../Database/Tables.md) | [Database/Schema.md](../Database/Schema.md)  
ERD: [System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## 15. Future Integration Direction

- DO.OneAccess will act as the authoritative identity source for future office systems.
- Future systems reference `UserId` (GUID) from DO.OneAccess.
- Full SSO/OIDC/OAuth is **not** in scope for v1 but the system must remain integration-ready.

See: [Integration/Overview.md](../Integration/Overview.md)

---

## 16. Documentation Index

| Section | Path |
|---|---|
| System Architecture | [System-Architecture/](../System-Architecture/Overview.md) |
| Security | [Security/](../Security/Security-Overview.md) |
| Access Control | [Access-Control/](../Access-Control/Overview.md) |
| Frontend | [Frontend/](../Frontend/Overview.md) |
| Backend | [Backend/](../Backend/Overview.md) |
| Database | [Database/](../Database/Schema.md) |
| Deployment | [Deployment/](../Deployment/Requirements.md) |
| Integration | [Integration/](../Integration/Overview.md) |
| Updates | [Updates/](../Updates/Update-Process.md) |

---

## 17. Documentation Rule & Canonical Source of Truth

`docs/DO.OneAccess-Specification-v1.md` is the **canonical source of truth** for approved architecture, database schema, entity identifiers, security rules, and business rules.

Detailed documents throughout `/docs` may expand on the specification but must not contradict it. If any detailed document conflicts with the Master Specification, the detailed document must be updated to conform to this Master Specification.

Any significant change involving Architecture, Security, Authentication, Authorization, Database, Access Control, Frontend architecture, Backend architecture, Deployment, or Integration **must** update the corresponding documentation under `/docs` as part of the same change.

> **Conflict Rule:** If an implementation requirement conflicts with an approved architectural decision documented here, **stop and report the conflict** instead of silently changing the architecture.

---

## 18. Finalized Architecture Decisions

The following decisions are **approved and locked**. They must not be silently changed. If implementation conflicts arise, stop and report.

### Decision 1 — Administrator System-Management Scope

`AdministratorSystemAccess` is a separate table that records which Systems an Administrator is authorized to manage. It is distinct from `UserSystemAccess` (user system usage) and `AdministratorScopes` (division scope).

| Table | Question Answered |
|---|---|
| `UserSystemAccess` | Can this **User use** this System? |
| `AdministratorSystemAccess` | Can this **Administrator manage** this System? |

- Only the System Administrator may grant `AdministratorSystemAccess` entries.
- Unique constraint on `(AdministratorUserId, SystemId)`.
- System Administrator does not require `AdministratorSystemAccess` (global authority).
- Backend authorization must verify `AdministratorSystemAccess` before allowing system-management operations.

See: [Access-Control/Administrator-Scope.md](Access-Control/Administrator-Scope.md)

### Decision 2 — Refresh Token Storage

- **Access Token:** JWT, short-lived, client memory only, sent in `Authorization: Bearer` header.
- **Refresh Token:** Delivered via `Set-Cookie` with `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`. Never in the JSON response body. Never in localStorage. Stored server-side as a cryptographic hash.
- **CSRF:** Required for auth cookie endpoints. Primary mitigation: `SameSite=Strict`.

See: [Security/Authentication.md](Security/Authentication.md) | [Security/JWT.md](Security/JWT.md)

### Decision 3 — API Response Design

- **Success:** Plain typed DTO responses (no wrapper).
- **Errors:** ASP.NET Core `ProblemDetails` (RFC 7807).
- **Not used:** Generic `Result<T>` wrapper as a mandatory API response envelope.

See: [Backend/API-Design.md](Backend/API-Design.md)

### Decision 4 — Role Management Governance & Boundary Rules

- **Role Transition Scope:** Through the administrative UI, System Administrators can modify user roles between `USER` and `ADMINISTRATOR` only.
- **System Administrator Assignment:** Assignment to or demotion from `SYSTEM_ADMINISTRATOR` is strictly prohibited via the normal UI.
- **Self-Role Modification:** System Administrators cannot modify their own role.
- **Initial Bootstrap:** The initial System Administrator bootstrap mechanism is implemented via dedicated secure first-run setup gateway (`POST /api/setup`) and client setup UI (`/setup`), protected by a one-time bootstrap token and permanently disabled once initialized.
- **Dedicated Endpoint:** Role modification is serviced by dedicated endpoint `PUT /api/users/{userId}/role` under `SystemAdministratorOnly` authorization policy.

### Decision 5 — Browser E2E Automation Status

- Real browser end-to-end (E2E) testing is designated as **PENDING — Environment Prerequisites Required**.
- In-process HTTP testing via `WebApplicationFactory<Program>`, component property testing, and mocked handler unit testing are strictly distinguished from browser E2E and are not classified as browser E2E.
- Real browser E2E requires a dedicated hosting runner, browser driver binaries (e.g., Playwright), isolated test database lifecycle management, and background Kestrel execution.

---

*DO.OneAccess Specification v1 — First Run Setup & Secure Bootstrap Complete (v1.3 — Architecture Decisions & Boundaries Locked)*
