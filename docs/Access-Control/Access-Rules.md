# Access Control — Access Rules

> **Section:** Access Control  
> **Document:** Access Rules  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [System-Access.md](System-Access.md) | [Administrator-Scope.md](Administrator-Scope.md) | [../Security/Authorization.md](../Security/Authorization.md)

---

## System Access Resolution Rules

These rules govern whether a User is permitted to access a registered System.

### Rule 1: Explicit Override Takes Priority

If a `UserSystemAccess` record exists for the `(UserId, SystemId)` pair, it takes full precedence over the System's default policy.

| AccessType | Result |
|---|---|
| `Allow` | Access **granted** regardless of DefaultAccess |
| `Deny` | Access **denied** regardless of DefaultAccess |

### Rule 2: Default Access Fallback

If no `UserSystemAccess` record exists, the System's `DefaultAccess` policy applies.

| DefaultAccess | Result |
|---|---|
| `All` | Access **granted** to all active users |
| `Restricted` | Access **denied** unless an explicit `Allow` exists |

### Rule 3: Inactive Users

Inactive Users (`IsActive = false`) must **not** have access to any system regardless of access rules.

### Rule 4: Inactive Systems

Access to an inactive System (`IsActive = false`) must be denied regardless of access rules.

---

## Access Resolution Flow

```
INPUT: UserId + SystemId

Is User active?
├── NO  → DENY
└── YES → Continue

Is System active?
├── NO  → DENY
└── YES → Continue

Does UserSystemAccess exist for (UserId, SystemId)?
├── YES (Allow) → GRANT
├── YES (Deny)  → DENY
└── NO          → Evaluate System.DefaultAccess
                   ├── All        → GRANT
                   └── Restricted → DENY
```

---

## Administrator Scope Rules

When an Administrator performs system-access management:

| Rule | Detail |
|---|---|
| Organizational scope | Can only manage Users within their assigned Division |
| System scope | Can only manage access to Systems within their management grant |
| Cross-scope denial | Any attempt to manage outside scope must be rejected with 403 |
| Cannot override own access | An Administrator should not use their admin rights to grant themselves access beyond what the System Administrator has authorized |

---

## Role Protection Rules

| Rule | Detail |
|---|---|
| Users cannot manage access | Users have no authority to modify `UserSystemAccess` |
| Users cannot change roles | Role assignment is restricted to System Administrators (and Administrators for User role within scope) |
| Administrators cannot elevate roles | Administrators cannot promote Users to Administrator or System Administrator |
| No self-elevation | No role may elevate itself |

---

## Audit Requirements

All changes to `UserSystemAccess`, `AdministratorScopes`, and `AdministratorSystemAccess` must be recorded in `AuditLogs`:

| Action | Must be logged |
|---|---|
| Grant system access | ✅ |
| Deny system access | ✅ |
| Revoke access override | ✅ |
| Assign Administrator scope | ✅ |
| Revoke Administrator scope | ✅ |
| Grant Administrator system-management rights | ✅ |
| Revoke Administrator system-management rights | ✅ |
| Role change | ✅ |
| User deactivation | ✅ |

---

## Constraint Summary

| Constraint | Table | Columns |
|---|---|---|
| Unique | `UserSystemAccess` | `(UserId, SystemId)` |
| Unique | `AdministratorSystemAccess` | `(AdministratorUserId, SystemId)` |
| Unique | `AdministratorScopes` | `AdministratorUserId` (one scope per Administrator) |
| FK | `UserSystemAccess` | `UserId`, `SystemId`, `CreatedBy` |
| FK | `AdministratorScopes` | `AdministratorUserId`, `DivisionId`, `CreatedBy` |
| FK | `AdministratorSystemAccess` | `AdministratorUserId`, `SystemId`, `CreatedBy` |

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| System Access | [System-Access.md](System-Access.md) |
| Administrator Scope | [Administrator-Scope.md](Administrator-Scope.md) |
| Roles | [Roles.md](Roles.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
