# Security — Authorization

> **Section:** Security  
> **Document:** Authorization Model  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Security-Overview.md](Security-Overview.md) | [../Access-Control/Roles.md](../Access-Control/Roles.md) | [../Access-Control/Access-Rules.md](../Access-Control/Access-Rules.md)

---

## Authorization Principle

**All authorization is enforced server-side by the API.** Client-side route guards and UI visibility controls are UX conveniences only — they are not a security boundary.

---

## Authorization Model

DO.OneAccess uses **role-based authorization** combined with **scope-based restrictions** for Administrators.

| Concept | Description |
|---|---|
| Role-Based | Each user has a role; controllers and endpoints are restricted by role code |
| Scope-Based | Administrators are additionally restricted to their assigned Division (`AdministratorScopes`) |
| Access-Based | System access is resolved per user against `UserSystemAccess` and `System.DefaultAccess` |

---

## Role Authorization

### System Administrator
- May access all administrative endpoints.
- Global scope; no Division/Section restriction.
- Enforced via `[Authorize(Roles = "SYSTEM_ADMINISTRATOR")]`.

### Administrator
- May access administrative endpoints scoped to their Division.
- Administrative scope derives strictly from `AdministratorScopes.DivisionId`. The Administrator's personal `SectionId` may be `NULL` and does not limit administrative reach.
- The server validates that the target entity (User or Section) belongs to the Administrator's assigned Division.
- Administrators may manage standard `USER` accounts within their division. They cannot create, modify, promote, demote, or manage `ADMINISTRATOR` or `SYSTEM_ADMINISTRATOR` accounts.
- Enforced via `[Authorize(Roles = "ADMINISTRATOR")]` plus scope validation logic in the Application layer.

### User
- May access user-facing endpoints only (e.g., view their own profile, view accessible systems).
- May not access any management endpoints.
- Enforced via `[Authorize(Roles = "USER")]` or simply `[Authorize]` where any authenticated user is permitted.

> **Semantic Rule:** `Code` (`SYSTEM_ADMINISTRATOR`, `ADMINISTRATOR`, `USER`) and `Name` (`System Administrator`, `Administrator`, `User`) are distinct. Role codes are used for programmatic authorization checks.

---

## Administrator Scope Enforcement

When an Administrator performs a management action, the Application layer must:

1. Retrieve the authenticated user's `UserId` from the JWT claims.
2. Look up the Administrator's assigned Division in `AdministratorScopes`.
3. Verify that the target entity (User or Section) belongs to that Division:
   - For a `User`, verify `user.RoleId == RoleType.User` and `user.Section.DivisionId == administratorScope.DivisionId`.
   - For a `Section`, verify `section.DivisionId == administratorScope.DivisionId`.
4. Reject the action with `HTTP 403 Forbidden` if the target is outside the Administrator's scope or is an administrative account.

This check is **not** optional and must not be bypassable by API consumers.

> **Response format:** Scope violations return `HTTP 403 Forbidden` with a `ProblemDetails` response body. See [../Backend/API-Design.md](../Backend/API-Design.md).

---

## System Access Authorization

When determining whether a User can access a System:

```
1. Does UserSystemAccess exist for (UserId, SystemId)?
   ├── YES → Apply the AccessType (Allow or Deny)
   └── NO  → Apply System.DefaultAccess (All or Restricted)
              ├── All        → ALLOW
              └── Restricted → DENY
```

This logic belongs in the Application layer (`ISystemAccessService`) and must be enforced server-side.

---

## Administrator System-Management Authorization

Administrators may only manage Systems that the System Administrator has explicitly granted them via `AdministratorSystemAccess` (`UNIQUE(AdministratorUserId, SystemId)`). This is tracked separately from `UserSystemAccess` and must be validated when an Administrator attempts to:

- View system management details.
- Grant/revoke user access to a system.

> See [../Access-Control/Administrator-Scope.md](../Access-Control/Administrator-Scope.md) for details.

---

## Role Elevation & System Administrator Protection

- Users cannot elevate their own role.
- Administrators cannot elevate their role or create/elevate users beyond `USER`.
- Only System Administrators can assign the `ADMINISTRATOR` role (requiring explicit Division scope).
- System Administrator assignment cannot be performed via standard role management or user creation endpoints.
- **Invariant:** The application guarantees the exactly-one System Administrator invariant for supported application workflows by restricting SysAdmin assignment to First Run Setup and the atomic Role Transfer workflow, protected by a Serializable transaction and post-condition validation.
- Role transfer (`POST /api/users/system-administrator/transfer`) requires an active System Administrator actor, validates target eligibility (active USER or ADMINISTRATOR), cleans up target administrator scopes and grants, assigns the departing SysAdmin to a valid USER or ADMINISTRATOR role, and revokes all refresh tokens.

---

## Authorization in Code

Authorization is applied at two levels:

| Level | Mechanism | Purpose |
|---|---|---|
| Controller | `[Authorize]`, `[Authorize(Roles = "...")]`, `[Authorize(Policy = "...")]` | Coarse-grained role check |
| Application layer | Service-level scope validation | Fine-grained scope and business rule enforcement |

Both levels must be applied. Controller-level alone is insufficient for scope enforcement.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Security Overview | [Security-Overview.md](Security-Overview.md) |
| Roles | [../Access-Control/Roles.md](../Access-Control/Roles.md) |
| Administrator Scope | [../Access-Control/Administrator-Scope.md](../Access-Control/Administrator-Scope.md) |
| Access Rules | [../Access-Control/Access-Rules.md](../Access-Control/Access-Rules.md) |
| Backend Authorization | [../Backend/Architecture.md](../Backend/Architecture.md) |
