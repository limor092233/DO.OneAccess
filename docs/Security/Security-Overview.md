# Security — Security Overview

> **Section:** Security  
> **Document:** Security Overview  
> **Related:** [Authentication.md](Authentication.md) | [JWT.md](JWT.md) | [Authorization.md](Authorization.md) | [Security-Guidelines.md](Security-Guidelines.md)

---

## Security Philosophy

DO.OneAccess is an authentication and access-control portal. Security is not an afterthought — it is a core requirement. The following principles govern all security decisions:

1. **Defense in depth** — Multiple layers of controls; no single point of failure.
2. **Server-side authority** — All authorization is enforced by the API. Client-side controls are UI conveniences only.
3. **Least privilege** — Users and Administrators have the minimum authority needed to perform their role.
4. **Auditability** — Significant actions are logged for review.
5. **Secret hygiene** — Secrets are never exposed in code, logs, or client-side configuration.

---

## Security Domain Coverage

| Area | Summary | Document |
|---|---|---|
| Authentication | Employee login, token issuance, session management | [Authentication.md](Authentication.md) |
| JWT | Access token format, lifetime, signing | [JWT.md](JWT.md) |
| Authorization | Role-based and scope-based access control | [Authorization.md](Authorization.md) |
| Password storage | Secure hashing; never plaintext | [Security-Guidelines.md](Security-Guidelines.md) |
| Refresh tokens | Hashed server-side; rotation and revocation | [Authentication.md](Authentication.md) |
| Audit logging | Audit trail rules and restrictions | [Security-Guidelines.md](Security-Guidelines.md) |
| Secret management | Configuration and secret handling | [Security-Guidelines.md](Security-Guidelines.md) |

---

## Security Baseline Requirements

### Passwords
- Must never be stored in plaintext.
- Must use a secure, modern password hashing algorithm (e.g., bcrypt or Argon2).
- Must never appear in logs, audit records, or API responses.

### JWT Access Tokens
- Must be short-lived (exact duration defined in [JWT.md](JWT.md)).
- Must be signed using a strong algorithm (e.g., HMAC-SHA256 or RSA).
- Signing keys must never be committed to source control.

### Refresh Tokens
- Must be stored server-side as cryptographic hashes only.
- Plaintext refresh tokens must never be logged or persisted.
- Must support revocation.
- Must have a defined expiry.

### Secrets and Configuration
- All application configuration must be stored in `appsettings` files.
- Development configuration and local signing keys use `appsettings.Development.json`. .NET User Secrets must NOT be used.
- Production configuration is stored in environment-specific appsettings files (e.g., `appsettings.Production.json`).
- The Blazor WebAssembly client must never contain server secrets.

### Audit Logs
- Must never contain passwords, raw tokens, or secrets.
- Record actor `UserId` (nullable GUID for system or unauthenticated events), action, `EntityName`, `EntityId`, `OldValues`, `NewValues`, IP address, user agent, and `CreatedAt` timestamp.

---

## Authorization Boundary

```
┌──────────────────────────────────────────┐
│      Blazor WebAssembly (Client)         │
│  - UI visibility controls (not security) │
│  - Route guards (UX only)               │
└─────────────────┬────────────────────────┘
                  │ JWT in Authorization header
                  ▼
┌──────────────────────────────────────────┐
│      ASP.NET Core API (Server)          │
│  ← AUTHORITATIVE SECURITY BOUNDARY →    │
│  - [Authorize] attribute enforcement     │
│  - Role-based policy checks             │
│  - Scope validation                      │
└──────────────────────────────────────────┘
```

> **Rule:** Hiding a UI element is **not** a security control. The API must independently enforce all authorization decisions.

---

## Initial Setup Security

- The first System Administrator is created through a **one-time secure initial setup process**.
- After successful initialization, the initial setup endpoint must be **permanently disabled** (cannot be re-triggered).
- Additional System Administrators must not be able to self-register.

---

## Related Documents

| Document | Link |
|---|---|
| Authentication | [Authentication.md](Authentication.md) |
| JWT | [JWT.md](JWT.md) |
| Authorization | [Authorization.md](Authorization.md) |
| Security Guidelines | [Security-Guidelines.md](Security-Guidelines.md) |
| Access Control Overview | [../Access-Control/Overview.md](../Access-Control/Overview.md) |
