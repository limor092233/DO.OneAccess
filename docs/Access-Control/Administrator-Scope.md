# Access Control — Administrator Scope

> **Section:** Access Control  
> **Document:** Administrator Scope  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Roles.md](Roles.md) | [Access-Rules.md](Access-Rules.md) | [../Security/Authorization.md](../Security/Authorization.md)

---

## Overview

An Administrator's management authority is **scoped** to exactly one Division. This scope is recorded in `AdministratorScopes` and is enforced server-side on every administrative action.

---

## Organizational Scope

```
AdministratorScopes
  └── AdministratorUserId → The Administrator (User)
  └── DivisionId          → The Division they may manage
  └── CreatedBy           → The System Administrator who granted this scope
```

An Administrator may:
- Manage all **Sections** within their assigned Division.
- Manage all **Users** (employee accounts) within those Sections.
- Grant/revoke system access for Users within their Division (for permitted systems only).

An Administrator must not:
- Manage any entity in another Division.
- Manage Divisions or Sections they have not been assigned.

---

## System-Management Scope (Separate Concept)

Beyond organizational scope, an Administrator may only manage systems that the System Administrator has **explicitly granted** to them. This is modeled in a dedicated table: `AdministratorSystemAccess`.

> **Decision (Approved):** Administrator system-management rights are stored in `AdministratorSystemAccess`. This is intentionally separate from `UserSystemAccess` (which controls User access to Systems) and from `AdministratorScopes` (which controls Division scope).

### Conceptual Relationship

```
Administrator User
    └── AdministratorSystemAccess
              └── System
```

### Key Distinctions

| Table | Question Answered |
|---|---|
| `UserSystemAccess` | Can this **User use** this System? |
| `AdministratorSystemAccess` | Can this **Administrator manage** this System? |

These are entirely separate concepts and must not be conflated.

**Rules:**
- Only the System Administrator may grant `AdministratorSystemAccess` entries.
- The Administrator may manage `UserSystemAccess` only for Systems listed in their `AdministratorSystemAccess`.
- The Administrator may manage User access only for Users within their assigned Division (`AdministratorScopes`).
- Both checks (Division scope AND system-management grant) must pass for any administrative system-access action.
- The Administrator cannot grant access beyond their organizational scope.
- The Administrator cannot manage systems they have not been granted management rights for.
- **System Administrator does not require `AdministratorSystemAccess`** — the System Administrator role carries global authority.

---

## Scope Enforcement (API)

When an Administrator performs any management action:

1. Retrieve the `UserId` of the authenticated Administrator from the JWT.
2. Look up the Administrator's `AdministratorScopes` record to identify their Division.
3. Resolve the Division of the target entity.
4. If the target entity's Division ≠ the Administrator's Division → **reject with 403 Forbidden**.
5. For system-management actions: additionally verify that an `AdministratorSystemAccess` record exists for `(AdministratorUserId, SystemId)`. If not → **reject with 403 Forbidden**.

Both checks (Division scope AND system-management grant) are required and must both pass. This enforcement is applied in the Application layer and cannot be bypassed via the API.

---

## AdministratorScopes Table

| Column | Type | Notes |
|---|---|---|
| `AdministratorScopeId` | `bigint` PK | Identity primary key |
| `AdministratorUserId` | `uniqueidentifier` FK | References `Users.UserId`; must be a User with Administrator role (`UNIQUE`) |
| `DivisionId` | `int` FK | References `Divisions.DivisionId` |
| `CreatedAt` | `datetime2` | Timestamp of creation |
| `CreatedBy` | `uniqueidentifier` FK | References `Users.UserId`; System Admin who granted scope |
| `UpdatedAt` | `datetime2` | Nullable |
| `UpdatedBy` | `uniqueidentifier` FK | Nullable |

---

## AdministratorSystemAccess Table

Records which Systems a specific Administrator is authorized to manage. Granted by the System Administrator.

| Column | Type | Notes |
|---|---|---|
| `AdministratorSystemAccessId` | `bigint` PK | Identity primary key |
| `AdministratorUserId` | `uniqueidentifier` FK | References `Users.UserId`; must be a User with Administrator role |
| `SystemId` | `uniqueidentifier` FK | References `Systems.SystemId` |
| `CreatedAt` | `datetime2` | Timestamp of creation |
| `CreatedBy` | `uniqueidentifier` FK | References `Users.UserId`; System Admin who granted management rights |
| `UpdatedAt` | `datetime2` | Nullable |
| `UpdatedBy` | `uniqueidentifier` FK | Nullable |

**Unique constraint:** `UNIQUE(AdministratorUserId, SystemId)` — duplicate Administrator + System entries are not permitted.

---

## Business Rules

| Rule | Detail |
|---|---|
| One scope per Administrator | An Administrator has exactly one Division scope |
| Scope granted by System Administrator only | Only System Administrators may assign Division scope |
| Scope change | Changing an Administrator's Division requires System Administrator action |
| No cross-division authority | An Administrator cannot manage any entity outside their Division |
| No self-scope modification | An Administrator cannot modify their own scope |
| System management scope separate | System-management rights are stored in `AdministratorSystemAccess`; not derived from `AdministratorScopes` alone |
| Duplicate system grant prohibited | `UNIQUE(AdministratorUserId, SystemId)` in `AdministratorSystemAccess` must be unique |
| System Administrator exempt | The System Administrator role has global authority; `AdministratorSystemAccess` does not apply |

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Roles | [Roles.md](Roles.md) |
| Access Control Overview | [Overview.md](Overview.md) |
| Access Rules | [Access-Rules.md](Access-Rules.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
