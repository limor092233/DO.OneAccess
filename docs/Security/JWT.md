# Security — JWT

> **Section:** Security  
> **Document:** JWT Configuration and Design  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Authentication.md](Authentication.md) | [Security-Overview.md](Security-Overview.md)

---

## JWT Access Token Overview

DO.OneAccess issues **JSON Web Tokens (JWT)** as access tokens. These are:

- Short-lived.
- Signed with a server-side secret key.
- Validated by the same server on each API request.
- Never stored server-side (stateless by design).

---

## Token Lifetime

| Token | Recommended Lifetime | Configurable |
|---|---|---|
| Access Token | 15 minutes | Yes (via configuration) |
| Refresh Token | 7 days | Yes (via configuration) |

Token lifetimes are defined in server configuration, not hardcoded.

---

## Token Structure (Claims)

The JWT access token includes the following claims:

| Claim | Key | Value |
|---|---|---|
| Subject | `sub` | `UserId` (GUID) |
| Role | `role` | Role code (`SYSTEM_ADMINISTRATOR`, `ADMINISTRATOR`, `USER`) |
| Username | `unique_name` / `preferred_username` | `Username` (login identifier) |
| Issued At | `iat` | Unix timestamp |
| Expiry | `exp` | Unix timestamp |
| Issuer | `iss` | Configured issuer (e.g., `DO.OneAccess`) |
| Audience | `aud` | Configured audience |

Additional claims may be included as needed (e.g., `EmployeeNumber` for business display, division scope for Administrators).

> **Security note:** Do not include sensitive data (passwords, hashes, secrets) in JWT claims. Claims are base64-encoded, not encrypted.

---

## Signing Algorithm

- Algorithm: **HMAC-SHA256 (HS256)** for symmetric signing.
- The signing key must be a cryptographically strong secret.
- The signing key must never be committed to source control.
- The signing key must be at least 256 bits (32 bytes) in length.

If asymmetric signing (RS256) is required for future integration scenarios, this can be adopted in a future version.

---

## Token Validation

The server validates every access token on each protected API request:

1. Verify the token is present in the `Authorization: Bearer <token>` header.
2. Verify the signature using the configured signing key.
3. Verify `iss` matches the configured issuer.
4. Verify `aud` matches the configured audience.
5. Verify the token has not expired (`exp`).
6. Extract claims and populate the ASP.NET Core `ClaimsPrincipal`.

---

## Configuration

JWT parameters are defined in `appsettings.json` (non-secret portions) and secrets management (signing key):

```json
// appsettings.json — shared baseline
{
  "Jwt": {
    "Issuer": "DO.OneAccess",
    "Audience": "DO.OneAccess.API",
    "AccessTokenLifetimeMinutes": 15,
    "RefreshTokenLifetimeDays": 7
  }
}
```

```json
// appsettings.Development.json — development configuration
{
  "Jwt": {
    "Key": "DevelopmentOnlyLocalSigningKeyAtLeast32BytesLong!"
  }
}
```

All application configuration must be stored in appsettings files. In development, `Jwt:Key` is stored in `appsettings.Development.json`. .NET User Secrets must NOT be used. In production, configuration is stored in environment-specific appsettings files.

---

## Security Rules

| Rule | Requirement |
|---|---|
| Signing key | Never committed to source control |
| Signing key | Minimum 256-bit strength |
| Claims | Must not contain passwords, tokens, or secrets |
| Token validation | Must validate signature, issuer, audience, and expiry |
| Access token storage (client) | Stored in memory only (C# field); never in a cookie, localStorage, or sessionStorage |
| Refresh token storage (client) | **Secure, HttpOnly, SameSite cookie only.** Must not be stored in localStorage, sessionStorage, or any JavaScript-accessible location. Must not be returned in the JSON response body. |
| Refresh token storage (server) | Stored as a cryptographic hash in `RefreshTokens` (`RefreshTokenId`, `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenId`). Never stored in plaintext. |
| Access token revocation | Not supported by design (stateless); use short lifetimes to mitigate |
| Refresh token revocation | Supported via `RevokedAt` timestamp and `ReplacedByTokenId` rotation tracking |
| CSRF protection | Required for cookie-based refresh/logout endpoints. Primary: `SameSite=Strict`. Secondary (if SameSite=Lax): CSRF token check. |

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Authentication | [Authentication.md](Authentication.md) |
| Security Overview | [Security-Overview.md](Security-Overview.md) |
| Security Guidelines | [Security-Guidelines.md](Security-Guidelines.md) |
