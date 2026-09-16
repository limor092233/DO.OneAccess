# Security — Authentication

> **Section:** Security  
> **Document:** Authentication  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [JWT.md](JWT.md) | [Security-Overview.md](Security-Overview.md)

---

## Overview

DO.OneAccess uses **JWT-based authentication** with short-lived access tokens and server-side hashed refresh tokens. Employees authenticate using their `Username` and password, and the system returns a token pair.

---

## Authentication Flow

```
Employee                  Client (WASM)               Server (API)
   │                           │                           │
   │──── Enter credentials ───▶│                           │
   │                           │──── POST /api/auth/login ────▶│
   │                           │     { username,           │
   │                           │       password }          │
   │                           │                           │
   │                           │         Validate creds    │
   │                           │         Hash password     │
   │                           │         Compare hash      │
   │                           │                           │
   │                           │◄─── 200 OK ────────────────│
   │                           │     { accessToken,        │
   │                           │       expiresIn }         │
   │                           │     Set-Cookie:           │
   │                           │       refreshToken        │
   │                           │       (HttpOnly, Secure,  │
   │                           │        SameSite=Strict)   │
   │                           │                           │
   │                           │    Store accessToken      │
   │                           │    in memory              │
   │◀── Redirect to dashboard ─│                           │
```

---

## Credential Validation & Identity Rules

1. Receive `Username` and `Password` from the login request.
2. Look up the `User` by `Username` (unique login identifier).
3. Verify the account is active (`IsActive = true`).
4. Compare the submitted password against the stored `PasswordHash` using the approved password hasher.
5. On failure: record login failure in `LoginHistories` (recording `UsernameAttempted`, `Success = false`, `FailureReason`, `IpAddress`, `UserAgent`, `CreatedAt`); return a generic error (do not reveal whether the account exists).
6. On success: update `LastLoginAt = DateTime.UtcNow`, issue token pair, record success in `LoginHistories`.

**Identity Field Distinctions:**
- `Username` is the unique login identifier.
- `EmployeeNumber` is the official employee/business identifier.
- `Email` is employee contact/identity information.
- Neither `EmployeeNumber` nor `Email` is used as the `Users` primary key.

---

## Token Pair

> **Decision (Approved):** Refresh tokens are delivered and transported via a Secure, HttpOnly, SameSite cookie. They must never be exposed to JavaScript or stored in localStorage.

| Token | Lifetime | Transport / Storage | Notes |
|---|---|---|---|
| Access Token (JWT) | Short-lived (e.g., 15 minutes) | Client memory only | Sent in `Authorization: Bearer` header; never in a cookie |
| Refresh Token | Longer-lived (e.g., 7 days) | Secure, HttpOnly, SameSite cookie (browser) + hashed in server DB (`RefreshTokens`) | Never exposed to JavaScript; never in localStorage |

---

## Refresh Token Handling

- The plaintext refresh token is generated server-side.
- It is **never returned in the JSON response body** and is **never accessible to JavaScript**.
- The server sets the refresh token in a **`Set-Cookie` header** with mandatory attributes:
  - `HttpOnly` — prevents JavaScript access.
  - `Secure` — transmitted over HTTPS only.
  - `SameSite=Strict` (preferred) — primary CSRF mitigation.
  - `Path=/api/auth` — scope the cookie to auth endpoints only.
- The server stores a **cryptographic hash** of the refresh token in `RefreshTokens` (`RefreshTokenId`, `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenId`, `CreatedByIp`, `RevokedByIp`). Plaintext tokens are never stored.
- On refresh, the browser automatically sends the HttpOnly cookie; the server extracts, hashes, and compares against the active stored token.
- **Rotation & Revocation:** Upon successful refresh, the old token's `RevokedAt` timestamp is set, `ReplacedByTokenId` references the new token, and a new token pair is issued.
- Do not replace this model with only `IsRevoked` unless a specific architectural reason is documented and approved.

---

## Session Termination (Logout)

- On logout, the client calls `POST /api/auth/logout`.
- The server reads the refresh token from the HttpOnly cookie, marks the corresponding `RefreshTokens` record with `RevokedAt = DateTime.UtcNow` and `RevokedByIp`, and clears the refresh cookie (via `Set-Cookie` with expired date).
- The short-lived access token will expire on its own (stateless JWT).
- The client clears its in-memory access token on logout.

---

## Initial Setup Authentication

The first System Administrator is created through a secure one-time initialization endpoint:

- The endpoint is only accessible when no System Administrator account exists in the database.
- It accepts initial administrator credentials (`Username`, `Password`, employee info) and creates the first System Administrator.
- After successful setup, the endpoint returns a permanently-disabled status for subsequent calls.
- The initialization process must be logged in `AuditLogs`.

---

## Login History Recording

Every login attempt (success or failure) is recorded in `LoginHistories`:

| Field | Type | Description |
|---|---|---|
| `LoginHistoryId` | `bigint` PK | Primary key |
| `UserId` | `uniqueidentifier` FK | The user who attempted login (null if user unresolved) |
| `UsernameAttempted` | `nvarchar(100)` | Username supplied during login attempt |
| `Success` | `bit` | `true` or `false` |
| `IpAddress` | `nvarchar(50)` | Caller IP address |
| `UserAgent` | `nvarchar(500)` | Caller user agent |
| `FailureReason` | `nvarchar(200)` | Reason if failed (generic; no password details) |
| `CreatedAt` | `datetime2` | Attempt timestamp |

> `LoginHistories` must never contain password values or token values.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| JWT Details | [JWT.md](JWT.md) |
| Security Overview | [Security-Overview.md](Security-Overview.md) |
| Security Guidelines | [Security-Guidelines.md](Security-Guidelines.md) |
| Frontend Auth Flow | [../Frontend/Authentication-Flow.md](../Frontend/Authentication-Flow.md) |
