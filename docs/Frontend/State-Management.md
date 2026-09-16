# Frontend — State Management

> **Section:** Frontend  
> **Document:** State Management  
> **Related:** [Architecture.md](Architecture.md) | [Authentication-Flow.md](Authentication-Flow.md)

---

## State Philosophy

DO.OneAccess uses a **simple, service-based state model**. There is no complex global state management library. State is managed through:

1. **`AuthenticationStateProvider`** — Owns authentication state (who is logged in, their role).
2. **Service-level caching** — Services may cache data for the current session where appropriate.
3. **Component-local state** — Component-specific data (form values, lists) is local to the component.

This approach avoids over-engineering while keeping state predictable and testable.

---

## Authentication State

The primary shared state is **authentication state**.

### `JwtAuthenticationStateProvider`

Extends `AuthenticationStateProvider` (Blazor's built-in abstraction):

- Holds the current access token in memory.
- Parses JWT claims to construct a `ClaimsPrincipal`.
- Exposes `GetAuthenticationStateAsync()` — called by Blazor's auth framework.
- Exposes `NotifyAuthenticationStateChanged(Task<AuthenticationState>)` — called after login/logout to trigger re-render.

```
JwtAuthenticationStateProvider
  ├── _accessToken (in-memory)
  ├── GetAuthenticationStateAsync() → ClaimsPrincipal from token
  ├── SetToken(string token) → update token + notify
  └── ClearToken() → clear token + notify
```

### Token Handling

- **Access Token:** Held in-memory within `JwtAuthenticationStateProvider` (C# field). It is short-lived and intentionally cleared on browser reload.
- **Refresh Token:** Stored exclusively in a `Secure`, `HttpOnly`, `SameSite` cookie managed automatically by the browser. It is not accessible to JavaScript or client code.

---

## Application-Level State (`AppState`)

An optional injectable `AppState` singleton may hold:
- Current user display name / username.
- Current user's accessible systems list (cached after login).

`AppState` is populated after login and cleared on logout. It is not a replacement for server-side authorization.

---

## Component-Local State

Pages and components manage their own data (loaded from services) as component fields (`private` C# state in code blocks). This covers:
- Lists of users, sections, systems, etc.
- Form model objects.
- Loading/error flags.

---

## State Flow on Login

```
AuthService.LoginAsync(username, password)
  → POST /api/auth/login
  → Server sets HttpOnly refresh token cookie
  → Store access token in JwtAuthenticationStateProvider
  → Call NotifyAuthenticationStateChanged()
     → Blazor re-renders with authenticated ClaimsPrincipal
     → NavMenu shows role-appropriate items
     → Route guard allows navigation to /dashboard
```

---

## State Flow on Logout

```
AuthService.LogoutAsync()
  → POST /api/auth/logout (server revokes token via RevokedAt and clears HttpOnly cookie)
  → Clear access token from JwtAuthenticationStateProvider
  → Call NotifyAuthenticationStateChanged()
     → Blazor re-renders as unauthenticated
     → Redirect to /login
```

---

## State on Page Reload

```
App startup
  → POST /api/auth/refresh (browser automatically attaches HttpOnly cookie)
      → Success: SetToken(accessToken) → authenticated state
      → Failure: ClearToken() → unauthenticated state
```

---

## No Client-Side Authorization State

The client does **not** store access rules, role assignments, or system permissions independently. These are always resolved server-side. The only role information the client holds is the `role` claim in the JWT token, used for UI display purposes only.

---

## Related Documents

| Document | Link |
|---|---|
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Frontend Architecture | [Architecture.md](Architecture.md) |
| Client Authorization | [Authorization.md](Authorization.md) |
