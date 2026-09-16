# Frontend — API Integration

> **Section:** Frontend  
> **Document:** API Integration  
> **Related:** [Architecture.md](Architecture.md) | [Authentication-Flow.md](Authentication-Flow.md) | [../Backend/API-Design.md](../Backend/API-Design.md)

---

## Overview

The Blazor WebAssembly client communicates with the DO.OneAccess API exclusively over HTTPS using typed `HttpClient` instances configured via Dependency Injection.

---

## HttpClient Configuration

The client registers distinct HTTP client pipelines in `Program.cs` / `ServiceCollectionExtensions.cs` to maintain clean separation and prevent circular dependencies:

1. **Unauthenticated Auth HttpClient (`IClientAuthService`):**
   - Configured with `BaseAddress` targeting `ApiBaseUrl`.
   - Does **not** attach `CustomAuthorizationMessageHandler`.
   - Used exclusively for `/api/auth/login`, `/api/auth/refresh`, and `/api/auth/logout`.

2. **Dedicated Unauthenticated Setup HttpClient (`ISetupApiClient`):**
   - Configured with `BaseAddress` targeting `ApiBaseUrl`.
   - Does **not** attach `CustomAuthorizationMessageHandler`.
   - Transports `X-Bootstrap-Token` header for `/api/setup` without Bearer token attachment or refresh attempts.
   - Complemented by scoped `ISetupStateService` which caches the initialization state in-memory.

3. **Authorized API HttpClient (Application Services):**
   - Configured with `BaseAddress` targeting `ApiBaseUrl`.
   - Attaches `CustomAuthorizationMessageHandler` to automatically supply the `Authorization: Bearer <token>` header and coordinate silent refresh on 401.
   - Injected into all typed application REST API clients (`SystemApiClient`, `UserApiClient`, etc.).

```csharp
// ServiceCollectionExtensions.cs
services.AddScoped<IClientAuthService>(sp =>
{
    var jsRuntime = sp.GetRequiredService<IJSRuntime>();
    var authHttpClient = new HttpClient { BaseAddress = baseUri };
    return new ClientAuthService(authHttpClient, jsRuntime);
});

services.AddScoped<ISetupApiClient>(sp =>
{
    var setupHttpClient = new HttpClient { BaseAddress = baseUri };
    return new SetupApiClient(setupHttpClient);
});

services.AddScoped<ISetupStateService, SetupStateService>();

services.AddTransient<CustomAuthorizationMessageHandler>();
services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<CustomAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = baseUri };
});
```

> `ApiBaseUrl` is configured in `appsettings.json` (client-side). For local development, this defaults to `http://localhost:5111`. The Server exposes a CORS policy (`BlazorClientPolicy`) configured in server `appsettings.json` under `Cors:AllowedOrigins` (allowing `http://localhost:5202` and `https://localhost:7030` with credentials).


---

## Authorization Message Handler

`AuthorizationMessageHandler` is a `DelegatingHandler` that:
1. Reads the current access token from `JwtAuthenticationStateProvider`.
2. Attaches the `Authorization: Bearer <token>` header to outgoing requests.
3. Passes the request to the next handler.

This ensures all API calls automatically carry the authentication token without each service needing to manage it.

---

## Service Pattern

Client services wrap API calls and are injected into pages and components:

```
ISetupApiClient   → /api/setup/* (unauthenticated, X-Bootstrap-Token)
IClientAuthService→ /api/auth/* (unauthenticated, HttpOnly cookies + CSRF)
IUserService      → /api/users/* (authorized)
ISystemService    → /api/systems/* (authorized)
IDivisionService  → /api/divisions/* (authorized)
ISectionService   → /api/sections/* (authorized)
IAuditService     → /api/audit-logs/* (authorized)
```

Services:
- Accept typed DTOs as parameters.
- Return typed response DTOs or result wrappers.
- Handle HTTP errors and surface them as typed exceptions or result types.

---

## Error Handling

Client services must handle common HTTP error scenarios:

| HTTP Status | Client Action |
|---|---|
| `200 OK` | Return typed result |
| `400 Bad Request` | Surface validation errors to the component |
| `401 Unauthorized` | Attempt token refresh; if fails, redirect to login |
| `403 Forbidden` | Surface as an authorization error; redirect to `/access-denied` |
| `404 Not Found` | Surface as a not-found error |
| `500 Internal Server Error` | Show generic error message; do not expose server details |

---

## JSON Serialization

- Use `System.Text.Json` (default in .NET 10).
- DTOs must be consistent with the API's serialization settings.
- Property naming convention: `camelCase` in JSON (configured via `JsonSerializerOptions`).

---

## Configuration

Client configuration is in `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

> **Security:** Client-side `appsettings.json` is **public** (downloaded to the browser). It must never contain secrets, API keys, signing keys, or connection strings.

---

## Related Documents

| Document | Link |
|---|---|
| Backend API Design | [../Backend/API-Design.md](../Backend/API-Design.md) |
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Frontend Architecture | [Architecture.md](Architecture.md) |
| Security Guidelines | [../Security/Security-Guidelines.md](../Security/Security-Guidelines.md) |
