# Access Control — Overview

> **Section:** Access Control  
> **Document:** Overview  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Roles.md](Roles.md) | [Administrator-Scope.md](Administrator-Scope.md) | [System-Access.md](System-Access.md) | [Access-Rules.md](Access-Rules.md)

---

## Purpose

DO.OneAccess implements distinct access-control dimensions:

1. **System access** — Whether a User can use a registered office system (`UserSystemAccess`).
2. **Administrative division scope** — Which organizational unit an Administrator can manage (`AdministratorScopes`).
3. **Administrative system-management scope** — Which systems an Administrator is authorized to manage (`AdministratorSystemAccess`).

> **Critical:** These are entirely separate concepts and must not be conflated in implementation.

---

## Roles

| Role | RoleId | Code | Authority |
|---|---|---|---|
| System Administrator | `1` | `SYSTEM_ADMINISTRATOR` | Global platform authority |
| Administrator | `2` | `ADMINISTRATOR` | Scoped to one Division |
| User | `3` | `USER` | Regular employee; accesses permitted systems |

Each user has exactly **one** role (`Users.RoleId`). `Code` and `Name` are distinct and not interchangeable. See [Roles.md](Roles.md) for full detail.

---

## Organizational Hierarchy

```
Division
  └── Section
        └── Employee
              └── User
```

- A Division contains one or more Sections.
- A Section belongs to exactly one Division. Section Code is unique within Division: `UNIQUE(DivisionId, Code)`.
- An Employee belongs to exactly one Section.
- An Employee has exactly one User account.

---

## Access Control Dimensions

### Dimension 1: System Usage Access

Controls whether a specific User can access a specific registered System.

```
UserSystemAccess (explicit override: Allow / Deny)
  OR
System.DefaultAccess (fallback policy: All / Restricted)
```

See [System-Access.md](System-Access.md) and [Access-Rules.md](Access-Rules.md).

### Dimension 2: Administrator Division Scope

Controls which Division (and its Sections and Users) an Administrator can manage.

```
AdministratorScopes → Division
```

See [Administrator-Scope.md](Administrator-Scope.md).

### Dimension 3: Administrator System Management Scope

Controls which Systems an Administrator is authorized to manage (granting or revoking access for users within their division).

```
AdministratorSystemAccess → System
```

See [Administrator-Scope.md](Administrator-Scope.md).

---

## Why They Are Separate

| Concept | Table | Controls |
|---|---|---|
| System Usage Access | `UserSystemAccess` | Can this User use this System? |
| Admin Division Scope | `AdministratorScopes` | Which Division can this Administrator manage? |
| Admin System Management | `AdministratorSystemAccess` | Which Systems can this Administrator manage? |

An Administrator's ability to **manage** a system is tracked in `AdministratorSystemAccess` via System Administrator grants — and is completely separate from the Administrator's own personal system usage access as a User (`UserSystemAccess`).

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Roles | [Roles.md](Roles.md) |
| Administrator Scope | [Administrator-Scope.md](Administrator-Scope.md) |
| System Access | [System-Access.md](System-Access.md) |
| Access Rules | [Access-Rules.md](Access-Rules.md) |
| Security Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
