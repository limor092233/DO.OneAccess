# Frontend — Overview

> **Section:** Frontend  
> **Document:** Overview  
> **Related:** [Architecture.md](Architecture.md) | [Authentication-Flow.md](Authentication-Flow.md) | [Authorization.md](Authorization.md)

---

## Technology

The DO.OneAccess frontend is a **Blazor WebAssembly (WASM)** single-page application running in the browser. It communicates exclusively with the DO.OneAccess ASP.NET Core Web API.

| Technology | Detail |
|---|---|
| Framework | Blazor WebAssembly (.NET 10) |
| Language | C# |
| Runtime | WebAssembly in browser |
| Communication | HTTP/HTTPS via `HttpClient` to the Server API |
| Authentication | JWT access token attached to API requests |
| Hosted by | `DO.OneAccess.Server` (ASP.NET Core static file hosting) or separate CDN |

---

## Responsibilities

The Client is responsible for:

1. Presenting the login interface.
2. Storing and attaching JWT access tokens to API requests.
3. Presenting the user's accessible systems on the dashboard.
4. Presenting role-appropriate administrative interfaces.
5. Redirecting unauthenticated or unauthorized users to appropriate pages.
6. Refreshing the access token using the refresh token before expiry.

The Client is **not** responsible for:

- Enforcing authorization (the API is authoritative).
- Storing server secrets.
- Making authorization decisions independently.

---

## User Personas and Views

| User Type | Primary View |
|---|---|
| Unauthenticated | Login page |
| User | Dashboard showing accessible systems |
| Administrator | Dashboard + scoped administration panel |
| System Administrator | Dashboard + full administration panel |

---

## Security Boundary

The client is **not a security boundary**. All role and access checks the client performs are UX conveniences to avoid unnecessary API calls and provide appropriate UI. The API enforces all real access decisions.

> See [Authorization.md](Authorization.md) for client authorization approach.

---

## Related Documents

| Document | Link |
|---|---|
| Architecture | [Architecture.md](Architecture.md) |
| Project Structure | [Project-Structure.md](Project-Structure.md) |
| UI Design | [UI-Design.md](UI-Design.md) |
| Navigation | [Navigation.md](Navigation.md) |
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Authorization | [Authorization.md](Authorization.md) |
| State Management | [State-Management.md](State-Management.md) |
| API Integration | [API-Integration.md](API-Integration.md) |
| Component Guidelines | [Component-Guidelines.md](Component-Guidelines.md) |
