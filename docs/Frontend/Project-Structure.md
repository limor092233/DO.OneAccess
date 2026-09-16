# Frontend — Project Structure

> **Section:** Frontend  
> **Document:** Project Structure  
> **Related:** [Architecture.md](Architecture.md) | [../System-Architecture/Project-Structure.md](../System-Architecture/Project-Structure.md)

---

## Client Project Layout

```
DO.OneAccess.Client/
├── Program.cs                                ← App entry point; DI registration; HttpClient config
├── App.razor                                 ← Root router with CascadingAuthenticationState & AuthorizeRouteView
├── _Imports.razor                            ← Global using directives
│
├── Auth/
│   └── ProblemDetailsException.cs            ← Typed RFC 7807 problem details exception
│
├── Components/                               ← Reusable Blazor components
│   ├── Common/
│   │   ├── ConfirmDialog.razor               ← Modal confirmation dialog
│   │   ├── EmptyState.razor                  ← Visual empty list placeholder
│   │   ├── ErrorMessage.razor                ← Alert banner with validation errors list
│   │   ├── Icon.razor                        ← SVG icon set without heavy dependencies
│   │   ├── LoadingSpinner.razor              ← Accessible loading indicator
│   │   ├── ModalDialog.razor                 ← Reusable modal container
│   │   ├── Pagination.razor                  ← Paged navigation controls
│   │   └── StatusBadge.razor                 ← Active / Inactive status pills
│   └── Layout/
│       ├── TopBar.razor                      ← User identity and logout actions
│       └── TopBar.razor.css
│
├── Handlers/
│   └── CustomAuthorizationMessageHandler.cs  ← Attaches Bearer token, excludes auth endpoints, SemaphoreSlim 401 refresh
│
├── Layout/
│   ├── MainLayout.razor                      ← Authenticated shell with conditional sidebar and TopBar
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor                         ← Role-based side navigation
│   └── NavMenu.razor.css
│
├── Pages/                                    ← Routable pages
│   ├── AccessDenied.razor                    ← 403 Access denied view
│   ├── Dashboard.razor                       ← Dynamic accessible systems grid
│   ├── FirstRunSetup.razor                   ← One-time first-run setup wizard
│   ├── Home.razor                            ← Root redirector to /dashboard
│   ├── Login.razor                           ← Unauthenticated login form
│   ├── NotFound.razor                        ← 404 Route not found
│   ├── RedirectToLogin.razor                 ← Redirect helper for unauthenticated requests
│   │
│   ├── Admin/                                ← Administrator & System Administrator management
│   │   ├── ManageSections.razor              ← Division-scoped section management
│   │   ├── ManageSystemAccess.razor          ← System override allow/deny grants
│   │   └── ManageUsers.razor                 ← User account management & employee records
│   │
│   └── SystemAdmin/                          ← System Administrator exclusive management
│       ├── AuditLogs.razor                   ← System-wide audit event viewer
│       ├── LoginHistories.razor              ← Authentication history & diagnostics
│       ├── ManageAdminScopes.razor           ← Division scopes and system management authority
│       ├── ManageDivisions.razor             ← Global division directory
│       └── ManageSystems.razor               ← Office systems registry & launch URLs
│
├── Extensions/
│   └── ServiceCollectionExtensions.cs        ← Dedicated unauth/auth client registrations
│
├── Services/
│   ├── Api/                                  ← Typed REST API clients
│   │   ├── IAdminScopeApiClient.cs / AdminScopeApiClient.cs
│   │   ├── IAuditApiClient.cs / AuditApiClient.cs
│   │   ├── IDivisionApiClient.cs / DivisionApiClient.cs
│   │   ├── ISectionApiClient.cs / SectionApiClient.cs
│   │   ├── ISetupApiClient.cs / SetupApiClient.cs
│   │   ├── ISystemAccessApiClient.cs / SystemAccessApiClient.cs
│   │   ├── ISystemApiClient.cs / SystemApiClient.cs
│   │   └── IUserApiClient.cs / UserApiClient.cs
│   ├── ClientAuthService.cs                  ← In-memory token management, login/refresh/logout
│   ├── IClientAuthService.cs
│   ├── ISetupStateService.cs                 ← Setup status tracking & loop prevention
│   └── SetupStateService.cs
│   ├── JwtAuthenticationStateProvider.cs     ← AuthenticationStateProvider mapping JWT claims
│   └── ProblemDetailsReader.cs               ← RFC 7807 ProblemDetails parser
│
└── wwwroot/
    ├── css/
    │   └── app.css                           ← Design tokens, cards, status pills, table polish
    ├── favicon.ico
    ├── icon-192.png
    ├── icon-512.png
    └── index.html                            ← SPA container
```

---

## Key Files

| File | Purpose |
|---|---|
| `Program.cs` | Configure services, HttpClient, auth state provider |
| `App.razor` | Root router; wraps in `CascadingAuthenticationState` |
| `JwtAuthenticationStateProvider.cs` | Custom `AuthenticationStateProvider`; reads and maps JWT claims |
| `CustomAuthorizationMessageHandler.cs` | `DelegatingHandler`; attaches `Authorization: Bearer` header and coordinates silent 401 refresh |
| `ClientAuthService.cs` | In-memory token management, login, refresh, and logout operations |
| `ProblemDetailsReader.cs` | RFC 7807 problem details parsing and error surfacing |

---

## Page-Role Matrix

| Page | Required Role |
|---|---|
| `Login.razor` | None (unauthenticated) |
| `Dashboard.razor` | Any authenticated |
| `Admin/*` | Administrator or System Administrator |
| `SystemAdmin/*` | System Administrator |
| `AccessDenied.razor` | Any (no auth required) |
| `NotFound.razor` | Any |

---

## Related Documents

| Document | Link |
|---|---|
| Frontend Architecture | [Architecture.md](Architecture.md) |
| Navigation | [Navigation.md](Navigation.md) |
| Component Guidelines | [Component-Guidelines.md](Component-Guidelines.md) |
| System-Wide Project Structure | [../System-Architecture/Project-Structure.md](../System-Architecture/Project-Structure.md) |
