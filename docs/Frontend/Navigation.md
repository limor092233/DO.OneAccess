# Frontend — Navigation

> **Section:** Frontend  
> **Document:** Navigation  
> **Related:** [UI-Design.md](UI-Design.md) | [Authorization.md](Authorization.md) | [Project-Structure.md](Project-Structure.md)

---

## Navigation Model

DO.OneAccess uses **Blazor's built-in client-side router** for navigation. All routes are defined in Blazor components using `@page` directives. The `<Router>` in `App.razor` handles all navigation.

---

## Route Map

| Route | Page | Minimum Role |
|---|---|---|
| `/setup` | `FirstRunSetup.razor` | None (accessible only when uninitialized) |
| `/` | `Home.razor` (redirects to `/systems`) | — |
| `/login` | `Login.razor` | None (unauthenticated) |
| `/systems` | `Dashboard.razor` (Registered Systems Portal) | Any authenticated |
| `/dashboard` | `Dashboard.razor` (Compatibility Route) | Any authenticated |
| `/admin/users` | `ManageUsers.razor` | Administrator |
| `/admin/employees` | `ManageUsers.razor` (Compatibility Route Alias) | Administrator |
| `/admin/divisions` | `ManageSections.razor` (Division & Child Sections) | Administrator |
| `/admin/sections` | `ManageSections.razor` (Compatibility Route Alias) | Administrator |
| `/admin/system-access` | `ManageSystemAccess.razor` | Administrator |
| `/sysadmin/divisions` | `ManageDivisions.razor` (Divisions & Child Sections Hierarchy) | System Administrator |
| `/sysadmin/systems` | `ManageSystems.razor` | System Administrator |
| `/sysadmin/scopes` | `ManageAdminScopes.razor` | System Administrator |
| `/sysadmin/audit-logs` | `AuditLogs.razor` | System Administrator |
| `/sysadmin/login-histories` | `LoginHistories.razor` | System Administrator |
| `/access-denied` | `AccessDenied.razor` | Any |
| `/not-found` | `NotFound.razor` | Any |

---

## Navigation Guard

All authenticated routes are protected using `<AuthorizeRouteView>`:

```razor
<!-- App.razor -->
<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
            <NotAuthorized>
                @if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    <RedirectToLogin />
                }
                else
                {
                    <RedirectToAccessDenied />
                }
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <NotFound />
    </NotFound>
</Router>
```

---

## Navigation Menu

The side navigation menu renders role-appropriate links organized under a 3-tier hierarchy:

```
PORTAL
└── Registered Systems (/systems)

ADMINISTRATION
├── Manage Users (/admin/users)
├── Manage Division (/sysadmin/divisions for SysAdmin, /admin/divisions for Admin)
└── System Access (/admin/system-access)

SYSTEM ADMINISTRATION
├── Admin Scopes (/sysadmin/scopes)
├── Audit Logs (/sysadmin/audit-logs)
└── Login History (/sysadmin/login-histories)
```

### Role Visibility Matrix

| Navigation Item | Route | User | Administrator | System Administrator |
|---|---|:---:|:---:|:---:|
| **Portal** | | | | |
| Registered Systems | `/systems` | ✓ | ✓ | ✓ |
| **Administration** | | | | |
| Manage Users | `/admin/users` | — | ✓ | ✓ |
| Manage Division | `/admin/divisions` (Admin) / `/sysadmin/divisions` (SysAdmin) | — | ✓ | ✓ |
| System Access | `/admin/system-access` | — | ✓ | ✓ |
| **System Administration** | | | | |
| Admin Scopes | `/sysadmin/scopes` | — | — | ✓ |
| Audit Logs | `/sysadmin/audit-logs` | — | — | ✓ |
| Login History | `/sysadmin/login-histories` | — | — | ✓ |

> [!NOTE]
> Dashboard has been removed from visible navigation for all roles. `/dashboard` remains active as an authenticated compatibility route mapping to the Registered Systems component.

Navigation items for roles the user does not have must not be rendered. This is a UX decision; the API enforces access server-side.

---

## Redirect Behavior

| Situation | Behavior |
|---|---|
| Uninitialized system state | Redirect any route to `/setup` |
| Initialized access to `/setup` | Redirect to `/login` |
| Service connection error on startup | Render inline service error banner with Retry (no redirect loops) |
| Unauthenticated access to protected route | Redirect to `/login` |
| Authenticated access to login page | Redirect to `/systems` |
| Root `/` navigation | Redirect to `/systems` |
| Insufficient role for a route | Redirect to `/access-denied` |
| Unknown route | Render `NotFound` page |

---

## Related Documents

| Document | Link |
|---|---|
| UI Design | [UI-Design.md](UI-Design.md) |
| Frontend Authorization | [Authorization.md](Authorization.md) |
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Frontend Project Structure | [Project-Structure.md](Project-Structure.md) |
