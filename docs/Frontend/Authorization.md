# Frontend — Authorization

> **Section:** Frontend  
> **Document:** Client Authorization  
> **Related:** [Authentication-Flow.md](Authentication-Flow.md) | [Navigation.md](Navigation.md) | [../Security/Authorization.md](../Security/Authorization.md)

---

## Client Authorization Principle

**Client-side authorization is UX only.** It controls what the user sees and which routes are accessible in the UI. It is NOT a security boundary. The API enforces all real authorization decisions.

---

## Blazor Authorization Mechanisms

### 1. `[Authorize]` Attribute on Pages

```razor
@page "/dashboard"
@attribute [Authorize]
```
Requires the user to be authenticated. Unauthenticated users are redirected to login.

```razor
@page "/sysadmin/divisions"
@attribute [Authorize(Roles = "SYSTEM_ADMINISTRATOR")]
```
Requires a specific role. Users with insufficient roles see the `<NotAuthorized>` content.

---

### 2. `<AuthorizeView>` Component

Used within components to conditionally render content based on auth state:

```razor
<AuthorizeView Roles="SYSTEM_ADMINISTRATOR">
    <Authorized>
        <!-- System Admin content -->
    </Authorized>
</AuthorizeView>

<AuthorizeView Roles="ADMINISTRATOR,SYSTEM_ADMINISTRATOR">
    <Authorized>
        <!-- Admin content -->
    </Authorized>
</AuthorizeView>
```

---

### 3. Cascading `Task<AuthenticationState>`

Components can inject `[CascadingParameter] Task<AuthenticationState> AuthenticationState` to make programmatic decisions based on the current user's claims.

---

## Role Claim Source

Role claims in the client are populated from the JWT access token by `JwtAuthenticationStateProvider`. The `role` claim in the token contains the canonical role code (`SYSTEM_ADMINISTRATOR`, `ADMINISTRATOR`, `USER`), mapped to the `ClaimTypes.Role` claim used by Blazor's `[Authorize(Roles = "...")]` mechanism.

---

## Navigation Menu Authorization

The navigation menu uses `<AuthorizeView>` to show/hide navigation sections:

| Menu Section | Condition |
|---|---|
| Dashboard | Always shown when authenticated |
| Administration | Shown for `ADMINISTRATOR` or `SYSTEM_ADMINISTRATOR` |
| System Administration | Shown for `SYSTEM_ADMINISTRATOR` only |

---

## Page-Level Authorization Summary

| Page | Attribute |
|---|---|
| `Dashboard.razor` | `[Authorize]` |
| `ManageUsers.razor` | `[Authorize(Roles = "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]` |
| `ManageSections.razor` | `[Authorize(Roles = "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]` |
| `ManageSystemAccess.razor` | `[Authorize(Roles = "ADMINISTRATOR,SYSTEM_ADMINISTRATOR")]` |
| `ManageDivisions.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |
| `ManageAdministrators.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |
| `ManageSystems.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |
| `ManageAdminScopes.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |
| `AuditLogs.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |
| `LoginHistories.razor` | `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]` |

---

## Important Reminders

> The API enforces scope. Even if an Administrator accesses `/admin/users` in the browser, the API will only return and accept modifications for Users within their assigned Division.

> Hiding a UI element is not authorization. The API must reject unauthorized requests regardless of what the client shows.

---

## Related Documents

| Document | Link |
|---|---|
| Server-Side Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| Authentication Flow | [Authentication-Flow.md](Authentication-Flow.md) |
| Navigation | [Navigation.md](Navigation.md) |
| Roles | [../Access-Control/Roles.md](../Access-Control/Roles.md) |
