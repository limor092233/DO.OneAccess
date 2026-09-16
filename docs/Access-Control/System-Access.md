# Access Control — System Access

> **Section:** Access Control  
> **Document:** System Access  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Access-Rules.md](Access-Rules.md) | [Overview.md](Overview.md) | [../System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## Overview

DO.OneAccess manages access to registered office systems. Each registered System has a default access policy, and individual Users may have explicit access overrides.

---

## Systems Table

Each registered System has:

| Field | Description |
|---|---|
| `SystemId` | Stable GUID; referenced by future integrations |
| `SystemCode` | Canonical unique short code (e.g., `HR`, `PAYROLL`) |
| `SystemName` | Canonical human-readable system name |
| `Description` | Description of the system |
| `BaseUrl` | URL used to link to the system |
| `IconUrl` | URL for system icon (nullable) |
| `DefaultAccess` | `All` or `Restricted` |
| `IsActive` | Whether the system is currently enabled |

---

## DefaultAccess Policy

Each System has a `DefaultAccess` field:

| Value | Meaning |
|---|---|
| `All` | All active users may access this system by default |
| `Restricted` | No user may access this system unless explicitly allowed |

---

## UserSystemAccess — Explicit Override

`UserSystemAccess` records an explicit per-user override for a specific system:

| Field | Description |
|---|---|
| `UserSystemAccessId` | `bigint` PK |
| `UserId` | The target user (`uniqueidentifier` FK → Users.UserId) |
| `SystemId` | The target system (`uniqueidentifier` FK → Systems.SystemId) |
| `AccessType` | `Allow` or `Deny` |
| `CreatedAt` | Record creation timestamp |
| `CreatedBy` | The actor who set this rule (`uniqueidentifier` FK → Users.UserId) |
| `UpdatedAt` | Last modification timestamp (nullable) |
| `UpdatedBy` | Modifier user identity (`uniqueidentifier` FK, nullable) |

**Unique constraint:** `UNIQUE(UserId, SystemId)` — only one rule per user per system.

---

## Access Resolution Logic

```
For a given (UserId, SystemId):

1. Does a UserSystemAccess record exist?
   ├── YES (Allow) → GRANT ACCESS
   ├── YES (Deny)  → DENY ACCESS
   └── NO          → Apply System.DefaultAccess
                      ├── All        → GRANT ACCESS
                      └── Restricted → DENY ACCESS
```

This logic is implemented in the Application layer (`ISystemAccessService`) and is authoritative.

---

## Key Concept Distinction

> **Decision (Approved):** User system **usage** access and Administrator system **management** scope are entirely separate concepts and separate tables.

| Table | Question Answered |
|---|---|
| `UserSystemAccess` | Can this **User use** this System? |
| `AdministratorSystemAccess` | Can this **Administrator manage** this System? |

These tables serve different purposes and must not be conflated. `AdministratorSystemAccess` is documented in [Administrator-Scope.md](Administrator-Scope.md).

---

## Who Can Manage System Access

| Actor | Can Manage |
|---|---|
| System Administrator | All systems, all users |
| Administrator | Systems within their management scope; Users within their Division |
| User | Cannot manage system access |

An Administrator may only modify `UserSystemAccess` records for:
- Users within their assigned Division.
- Systems within their Administrator system-management grant (`AdministratorSystemAccess`).

---

## System Registration

- Only System Administrators can register new Systems.
- Systems must be registered before access rules can be configured.
- A System's `DefaultAccess` policy is set at registration and may be updated by authorized actors.

---

## Deactivation

- Deactivating a System (`IsActive = false`) should suspend access without deleting records.
- `UserSystemAccess` records are retained and take effect if the system is reactivated.

---

## Related Documents

| Document | Link |
|---|---|
| Access Rules | [Access-Rules.md](Access-Rules.md) |
| Administrator Scope | [Administrator-Scope.md](Administrator-Scope.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| Tables | [../Database/Tables.md](../Database/Tables.md) |
