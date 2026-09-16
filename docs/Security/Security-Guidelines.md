# Security — Security Guidelines

> **Section:** Security  
> **Document:** Security Guidelines  
> **Related:** [Security-Overview.md](Security-Overview.md) | [Authentication.md](Authentication.md) | [JWT.md](JWT.md)

---

## Password Security

| Requirement | Rule |
|---|---|
| Storage | Must use a secure password hashing algorithm (e.g., bcrypt, Argon2) |
| Plaintext | Passwords must never be stored in plaintext |
| Logging | Passwords must never appear in logs, audit records, or API responses |
| Transmission | Passwords must only be transmitted over HTTPS |
| Comparison | Always use constant-time comparison when verifying hashes |
| Work factor | Hashing work factor (cost) should be tuned for server capacity and security |

---

## Refresh Token Security

> **Decision (Approved):** Refresh tokens must be stored in a Secure, HttpOnly, SameSite cookie. They must not be stored in localStorage, sessionStorage, or any JavaScript-accessible location.

| Requirement | Rule |
|---|---|
| Server storage | Store only a cryptographic hash of the refresh token; never the plaintext token |
| Client transport | Delivered and transmitted exclusively via a Secure, HttpOnly, SameSite cookie |
| Not in localStorage | Refresh tokens must never be stored in localStorage or sessionStorage |
| Not in response body | Refresh tokens must never appear in a JSON response body |
| Not accessible to JavaScript | The HttpOnly attribute ensures JavaScript cannot read the refresh token |
| Cookie attributes | Must set: `HttpOnly`, `Secure`, `SameSite=Strict` (or `Lax` with additional CSRF token), `Path=/api/auth` |
| CSRF protection | Required. Primary: `SameSite=Strict`. Secondary: CSRF token for state-mutating auth operations if `SameSite=Lax` is used |
| Logging | Plaintext refresh tokens must never appear in logs or audit records |
| Rotation | Issue a new token pair on each successful refresh; revoke the old token; set new cookie |
| Revocation | Support explicit revocation via timestamp `RevokedAt = UTC now` and optional `ReplacedByTokenId`; clear cookie on logout |
| Expiry | Refresh tokens must have a defined expiry (`ExpiresAt`) |

---

## Configuration and Secret Management

All application configuration must be stored in `appsettings` files.

| Environment | Mechanism |
|---|---|
| Development | `appsettings.Development.json` (.NET User Secrets must NOT be used) |
| Staging / Production | Environment-specific appsettings files (e.g., `appsettings.Production.json`) |

**Rules:**
- All application configuration must be stored in `appsettings` files.
- Development `Jwt:Key` must be stored in `appsettings.Development.json`.
- .NET User Secrets must NOT be used for application configuration.
- Never include secrets in client-side Blazor configuration (it is sent to the browser).

---

## Configuration Security

```json
// appsettings.json — SAFE to commit (no secrets)
{
  "Jwt": {
    "Issuer": "DO.OneAccess",
    "Audience": "DO.OneAccess.API",
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "-- Set via environment/secrets --"
  }
}
```

- Connection strings with passwords must not be committed.
- JWT signing keys must not be committed.

---

## Audit Log Security

| Rule | Detail |
|---|---|
| No passwords | Audit logs must never contain password values |
| No tokens | Audit logs must never contain JWT or refresh token values |
| No secrets | Audit logs must never contain secrets or sensitive credentials |
| Safe fields | Record: `UserId` (nullable GUID), `Action`, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `UserAgent`, and `CreatedAt` |

---

## HTTPS

- All communication between Client and Server must use HTTPS.
- HTTP should be redirected to HTTPS in production.
- Development may use HTTP with developer certificates.

---

## API Security Headers

The following HTTP security headers should be applied in production:

| Header | Recommended Value |
|---|---|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Content-Security-Policy` | Configured appropriately for Blazor WASM |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |

---

## Error Handling

- API errors must return generic messages that do not reveal internal details.
- Do not expose stack traces in production responses.
- Do not reveal whether a user account exists when returning login failure messages (return a uniform "Invalid credentials" response).

---

## Role Elevation Protection

- Users cannot modify their own role.
- Administrators cannot elevate their own role or the roles of others beyond `User`.
- Role assignments are a System Administrator-only action.

---

## Initial Setup Hardening

- The initial setup endpoint must permanently disable itself after the first System Administrator is created.
- A guard check (e.g., check if any System Administrator exists in the database) must prevent re-initialization.
- This check must be server-side.

---

## Developer Checklist

Before submitting any change touching authentication, authorization, or data access:

- [ ] No secrets in source code.
- [ ] No plaintext passwords in any storage, log, or response.
- [ ] No plaintext tokens in any storage, log, or response.
- [ ] Refresh token not present in JSON response body.
- [ ] Refresh token cookie has `HttpOnly`, `Secure`, `SameSite`, and `Path` attributes set.
- [ ] CSRF mitigations applied for auth cookie endpoints.
- [ ] Authorization enforced at the API level (not only on the client).
- [ ] Input validated before processing.
- [ ] Errors return generic messages in production.
- [ ] Audit log entries do not include sensitive values.

---

## Related Documents

| Document | Link |
|---|---|
| Security Overview | [Security-Overview.md](Security-Overview.md) |
| Authentication | [Authentication.md](Authentication.md) |
| JWT | [JWT.md](JWT.md) |
| Authorization | [Authorization.md](Authorization.md) |
