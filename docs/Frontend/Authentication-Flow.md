# Frontend — Authentication Flow

> **Section:** Frontend  
> **Document:** Authentication Flow  
> **Related:** [Architecture.md](Architecture.md) | [State-Management.md](State-Management.md) | [../Security/Authentication.md](../Security/Authentication.md)

---

## Overview

The Blazor WebAssembly client implements the client-side half of the JWT authentication flow. The server is authoritative for all security decisions.

---

## Login Flow

```
User fills login form
         │
         ▼
AuthService.LoginAsync(username, password)
         │
         ▼
POST /api/auth/login
{ username, password }
         │
         ├── 200 OK → { accessToken, expiresIn }
         │    │    Set-Cookie: refreshToken (HttpOnly, Secure, SameSite=Strict)
         │    │
         │    ▼
         │   Store accessToken in memory (C# field)
         │   (Refresh token is in the cookie; not accessible to JavaScript)
         │   Notify JwtAuthenticationStateProvider
         │   Redirect to /dashboard
         │
         └── 401 Unauthorized
              │
              ▼
             Show generic error message
             (Do not reveal account existence)
```

---

## Token Storage

> **Decision (Approved):** The refresh token is stored exclusively in a Secure, HttpOnly, SameSite cookie set by the server. It is not accessible to JavaScript and must not be stored in localStorage or sessionStorage.

| Token | Storage Location | Rationale |
|---|---|---|
| Access Token | In-memory (C# field in `AuthService`) | Short-lived; lost on page reload (acceptable; refresh cookie restores session automatically) |
| Refresh Token | Secure, HttpOnly, SameSite cookie (managed by the browser) | Never accessible to JavaScript; protected from XSS; browser sends it automatically to auth endpoints |

---

## Session Restoration on Page Reload

On application startup:
1. Attempt `POST /api/auth/refresh` (browser automatically sends the HttpOnly refresh cookie if present).
2. On success: store new access token in memory; update auth state; redirect to dashboard if on login page.
3. On failure (cookie absent, token expired, or revoked): ensure auth state is unauthenticated; stay on or redirect to login.

---

## Token Refresh During Session

The client must refresh the access token before it expires:
1. Read the JWT expiry (`exp` claim) from the access token.
2. Set a timer to refresh shortly before expiry (e.g., 1 minute before).
3. On timer: call `POST /api/auth/refresh` (browser automatically sends the refresh cookie).
4. On success: update in-memory access token; update auth state.
5. On failure: force logout (clear access token; redirect to login).

---

## Logout Flow

```
User clicks Logout
         │
         ▼
AuthService.LogoutAsync()
         │
         ▼
POST /api/auth/logout
(No body needed; server reads refresh token from HttpOnly cookie)
         │
         ▼
Server revokes the token (sets RevokedAt timestamp)
Server clears the cookie (Set-Cookie with expired date)
         │
         ▼
Clear in-memory access token
Notify JwtAuthenticationStateProvider (unauthenticated state)
Redirect to /login
```

Logout is initiated client-side but the server-side revocation and cookie clearing are the security-critical steps.

---

## AuthenticationStateProvider

`JwtAuthenticationStateProvider` extends Blazor's `AuthenticationStateProvider`:

- Reads the stored access token.
- Parses JWT claims to construct the `ClaimsPrincipal`.
- Returns an authenticated state when a valid token is present.
- Returns an unauthenticated state when no token is available.
- Exposes `NotifyAuthenticationStateChanged()` to trigger re-renders on login/logout.

---

## Error Handling

| Scenario | Client Behavior |
|---|---|
| Login failure (401) | Show generic "Invalid credentials" message |
| Refresh failure | Clear session; redirect to login |
| API returns 401 on request | Attempt refresh; if fails, redirect to login |
| API returns 403 on request | Redirect to `/access-denied` |

---

## Related Documents

| Document | Link |
|---|---|
| Server-Side Authentication | [../Security/Authentication.md](../Security/Authentication.md) |
| JWT | [../Security/JWT.md](../Security/JWT.md) |
| State Management | [State-Management.md](State-Management.md) |
| Frontend Architecture | [Architecture.md](Architecture.md) |
