# Frontend — Architecture

> **Section:** Frontend  
> **Document:** Architecture  
> **Related:** [Overview.md](Overview.md) | [State-Management.md](State-Management.md) | [API-Integration.md](API-Integration.md)

---

## Blazor WebAssembly Architecture

DO.OneAccess uses **Blazor WebAssembly** — a client-side SPA that runs C# in the browser via WebAssembly. The client is downloaded from the server on first load and executes entirely in the browser, making API calls over HTTPS.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Browser (WASM)                       │
│                                                         │
│  ┌────────────────┐   ┌──────────────┐   ┌──────────┐  │
│  │  Blazor Pages  │──▶│   Services   │──▶│  State   │  │
│  │  & Components  │   │  (API calls) │   │  (Auth)  │  │
│  └────────────────┘   └──────┬───────┘   └──────────┘  │
│                              │                          │
└──────────────────────────────┼──────────────────────────┘
                               │ HTTPS + JWT
                               ▼
                    ┌──────────────────────┐
                    │  DO.OneAccess Server  │
                    │  (ASP.NET Core API)   │
                    └──────────────────────┘
```

---

## Key Architectural Layers (Client)

### Pages
Routable Razor components. Each page corresponds to a URL route. Pages compose Components.

### Components
Reusable UI building blocks. Components should be focused and presentational where possible.

### Services
C# classes that encapsulate API communication logic. Pages/Components call Services; Services call the API via `HttpClient`.

### State
Authentication state and any shared application state is managed in dedicated state objects injected via DI. The primary state concern is the authenticated user's identity and claims.

---

## Authentication State Architecture

```
AuthenticationStateProvider (custom)
  └── Reads JWT from storage
  └── Exposes ClaimsPrincipal to Blazor's CascadingAuthenticationState
  └── Triggers re-render on auth state change
```

The `AuthenticationStateProvider` is the authoritative source of authentication state in the Blazor app. All `<AuthorizeView>` and `[Authorize]` usage on client pages reads from this provider.

---

## Dependency Direction (Client)

```
Pages → Components → Services → HttpClient → API
         ↓
       State (injected)
```

Pages and Components do not call `HttpClient` directly; they go through Services.

---

## Token Attachment

All authenticated API requests must include the JWT access token in the `Authorization` header:

```
Authorization: Bearer <access_token>
```

This is handled by a delegating `HttpMessageHandler` that reads the token from the current authentication state and attaches it automatically.

---

## Token Refresh

The client must proactively refresh the access token before it expires:
- Monitor token expiry.
- Use the refresh token to request a new access token via `POST /auth/refresh`.
- Update the stored token and authentication state.
- Handle refresh failures (token expired/revoked) by redirecting to login.

---

## Routing and Navigation

- Blazor's built-in router handles client-side navigation.
- Route guards are implemented using `<AuthorizeRouteView>` and custom authorization checks.
- Unauthenticated access to protected routes redirects to the login page.
- Unauthorized role access redirects to an appropriate access-denied page.

---

## Related Documents

| Document | Link |
|---|---|
| Frontend Overview | [Overview.md](Overview.md) |
| Project Structure | [Project-Structure.md](Project-Structure.md) |
| State Management | [State-Management.md](State-Management.md) |
| API Integration | [API-Integration.md](API-Integration.md) |
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Authorization | [Authorization.md](Authorization.md) |
