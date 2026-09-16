# Backend — API Design

> **Section:** Backend  
> **Document:** API Design  
> **Related:** [Architecture.md](Architecture.md) | [../Frontend/API-Integration.md](../Frontend/API-Integration.md)

---

## API Style

- RESTful HTTP API.
- JSON request and response bodies.
- `camelCase` property names in JSON.
- Versioning strategy: TBD in implementation phase (recommend `/api/v1/` prefix).

---

## Base URL

```
https://{host}/api/
```

---

## Authentication

- All API endpoints except `/api/setup/*`, `/api/auth/login`, and `/api/auth/logout` require a valid JWT in the `Authorization: Bearer <token>` header.
- The initial setup endpoints (`/api/setup/*`) are unauthenticated (`[AllowAnonymous]`). State is verified dynamically against `Users.Any(u => u.RoleId == 1)`. Submissions to `POST /api/setup` require a valid `X-Bootstrap-Token` HTTP header and are rate-limited.

---

## Endpoint Groups

### Setup (`/api/setup`)

| Method | Endpoint | Auth | Role | Description |
|---|---|---|---|---|
| GET | `/api/setup/status` | None | — | Query dynamic initialization status (`no-store`) |
| POST | `/api/setup` | None (`X-Bootstrap-Token`) | — | One-time System Admin bootstrap (rate limited) |

---

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Role | Description |
|---|---|---|---|---|
| POST | `/api/auth/login` | None | — | Authenticate and receive token pair |
| POST | `/api/auth/refresh` | None (CSRF) | — | Refresh access token |
| POST | `/api/auth/logout` | None (CSRF) | Any | Revoke refresh token (cookie-based, idempotent) |

> **Note on Logout Authentication:** The logout endpoint permits unauthenticated requests (`[AllowAnonymous]`) because an expired Bearer token must not prevent a client from clearing its session and revoking its refresh token. Security is maintained via HttpOnly cookie validation and the `X-CSRF-TOKEN` header.

---

### Users (`/api/users`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/users` | Admin / SysAdmin | List users (scoped for Admin) |
| GET | `/api/users/{userId}` | Admin / SysAdmin | Get user by ID (GUID) |
| POST | `/api/users` | Admin / SysAdmin | Create user (Admin: User in scoped Division/Section; SysAdmin: User or Admin with Division scope; never SysAdmin) |
| POST | `/api/users/system-administrator/transfer` | SysAdmin (Active) | Atomic transfer of System Administrator role to an active User or Administrator |
| PUT | `/api/users/{userId}` | Admin / SysAdmin | Update user profile and status |
| PUT | `/api/users/{userId}/role` | SysAdmin | Update user role (between USER and ADMINISTRATOR only) |
| DELETE | `/api/users/{userId}` | Admin / SysAdmin | Deactivate user (soft delete; self-deactivation prevented) |

---

### Employees (`/api/employees` — Retired)

> **Architectural Decision:** Retired and consolidated into `/api/users` under the rule: **ONE EMPLOYEE = ONE DO.OneAccess USER**. Employee profile operations are handled through `/api/users`.

---

### Divisions (`/api/divisions`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/divisions` | Admin / SysAdmin | List divisions (SysAdmin: all; Administrator: scoped division) |
| GET | `/api/divisions/{divisionId}` | Admin / SysAdmin | Get division (int ID) |
| POST | `/api/divisions` | SysAdmin | Create division |
| PUT | `/api/divisions/{divisionId}` | SysAdmin | Update division |
| DELETE | `/api/divisions/{divisionId}` | SysAdmin | Deactivate division |

---

### Sections (`/api/sections`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/sections` | Admin / SysAdmin | List sections (scoped for Admin) |
| GET | `/api/sections/{sectionId}` | Admin / SysAdmin | Get section (int ID) |
| POST | `/api/sections` | Admin / SysAdmin | Create section |
| PUT | `/api/sections/{sectionId}` | Admin / SysAdmin | Update section |
| DELETE | `/api/sections/{sectionId}` | Admin / SysAdmin | Deactivate section |

---

### Systems (`/api/systems`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/systems` | Any authenticated | List accessible systems for current user |
| GET | `/api/systems/all` | SysAdmin | List all registered systems |
| GET | `/api/systems/{systemId}` | Admin / SysAdmin | Get system detail (GUID) |
| POST | `/api/systems` | SysAdmin | Register system |
| PUT | `/api/systems/{systemId}` | SysAdmin | Update system |
| DELETE | `/api/systems/{systemId}` | SysAdmin | Deactivate system |

---

### System Access (`/api/system-access`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/system-access` | Admin / SysAdmin | List access overrides (scoped) |
| POST | `/api/system-access` | Admin / SysAdmin | Set user system access override (Allow / Deny) |
| DELETE | `/api/system-access/{id}` | Admin / SysAdmin | Revoke access override (bigint ID) |

---

### Administrator Scopes (`/api/admin-scopes`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/admin-scopes` | SysAdmin | List all admin scopes |
| POST | `/api/admin-scopes` | SysAdmin | Assign administrator scope |
| DELETE | `/api/admin-scopes/{administratorScopeId}` | SysAdmin | Remove administrator scope (bigint ID) |

---

### Administrator System Access (`/api/admin-system-access`)

Manages which Systems an Administrator is authorized to manage. Separate from user system access.

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/admin-system-access` | SysAdmin | List all administrator system-management grants |
| POST | `/api/admin-system-access` | SysAdmin | Grant an Administrator management rights over a System |
| DELETE | `/api/admin-system-access/{administratorSystemAccessId}` | SysAdmin | Revoke an Administrator's management rights over a System (bigint ID) |

---

### Audit Logs (`/api/audit-logs`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/audit-logs` | SysAdmin | List audit log entries |

---

### Login Histories (`/api/login-histories`)

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/api/login-histories` | SysAdmin | List login history entries |

---

## HTTP Status Code Conventions

| Status | Usage |
|---|---|
| 200 OK | Successful GET / PUT |
| 201 Created | Successful POST (new resource created) |
| 204 No Content | Successful DELETE |
| 400 Bad Request | Validation failure |
| 401 Unauthorized | Missing or invalid token |
| 403 Forbidden | Authenticated but insufficient role or scope |
| 404 Not Found | Resource not found |
| 409 Conflict | State conflict or unique constraint violation (e.g., duplicate EmployeeNumber, setup already completed) |
| 429 Too Many Requests | Rate limit exceeded (e.g., POST /api/setup) |
| 503 Service Unavailable | Service unavailable (e.g., bootstrap token unconfigured in production) |
| 500 Internal Server Error | Unexpected server error (generic message) |

---

## API Response Design

> **Decision (Approved):** Successful responses return plain typed DTOs (or collections of DTOs) directly. Error responses use ASP.NET Core `ProblemDetails`. There is no generic `Result<T>` wrapper as a mandatory API response envelope.

### Success Responses

Successful operations return the resource or collection directly, without wrapping:

| Status | Response Body |
|---|---|
| 200 OK | Typed DTO or collection of DTOs |
| 201 Created | Typed DTO of the created resource |
| 204 No Content | _(empty body)_ |

**Example — Successful resource:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "userId": "00000000-0000-0000-0000-000000000001",
  "employeeNumber": "EMP001",
  "username": "jane.doe",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@office.gov"
}
```

### Error Responses

All API errors return ASP.NET Core `ProblemDetails` (RFC 7807). Do not use a custom error envelope.

| Status | Scenario | Response Body |
|---|---|---|
| 400 Bad Request | Validation failure | `ProblemDetails` with `errors` extension |
| 401 Unauthorized | Missing or invalid token | `ProblemDetails` |
| 403 Forbidden | Authenticated but insufficient role or scope | `ProblemDetails` |
| 404 Not Found | Resource not found | `ProblemDetails` |
| 409 Conflict | Unique constraint violation | `ProblemDetails` |
| 500 Internal Server Error | Unexpected server error | `ProblemDetails` (generic; no stack trace in production) |

**Example — Validation failure (400):**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "employeeNumber": ["Employee number is required."],
    "email": ["Email must be a valid email address."]
  }
}
```

**Example — Authorization failure (403):**
```http
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Forbidden",
  "status": 403
}
```

> **Architecture rule:** Do not introduce a generic `Result<T>` wrapper as a mandatory API response envelope. If a future implementation choice conflicts with this decision, stop and report the conflict before changing the design.

---

## Related Documents

| Document | Link |
|---|---|
| Backend Architecture | [Architecture.md](Architecture.md) |
| Frontend API Integration | [../Frontend/API-Integration.md](../Frontend/API-Integration.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| Access Rules | [../Access-Control/Access-Rules.md](../Access-Control/Access-Rules.md) |
