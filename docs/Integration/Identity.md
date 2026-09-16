# Integration — Identity

> **Section:** Integration  
> **Document:** Identity  
> **Related:** [Overview.md](Overview.md) | [Integration-Contract.md](Integration-Contract.md) | [../System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## DO.OneAccess as Identity Authority

DO.OneAccess is the **authoritative identity source** for users and systems within the office. Future office systems integrate with DO.OneAccess by referencing its stable identifiers.

---

## Identity Identifiers

| Identifier | Type | Table | Purpose |
|---|---|---|---|
| `UserId` | GUID | `Users.UserId` | Single stable technical user identity for authentication, authorization, and integration references |
| `SystemId` | GUID | `Systems.SystemId` | Stable system identity for integration references |
| `EmployeeNumber` | string | `Users.EmployeeNumber` | Official business-facing employee identifier |

---

## Identity Stability Rules

| Rule | Detail |
|---|---|
| `UserId` never changes | Once assigned, a `UserId` is permanent for the lifetime of the account |
| `SystemId` never changes | Once assigned, a `SystemId` is permanent |
| No reuse | A `UserId` or `SystemId` is never reassigned to a different entity |
| Soft delete preserves identity | Deactivated users/systems retain their IDs; they are not hard-deleted while integrations may reference them |
| Email/username can change | External systems must not use email or username as a user reference |

---

## Why GUID Identity for Integration Entities

Under the tiered identifier strategy, GUIDs are scoped strictly to externally meaningful and integration-facing entities (`Users`, `Systems`):
- They are globally unique; no coordination needed across systems.
- They are collision-resistant across distributed contexts.
- They do not reveal record counts or sequential ordering (unlike sequential integers).
- They are portable across systems without re-mapping.

Internal reference structures (`Roles`, `Divisions`, `Sections`, scopes, junctions, and logs) use integer types (`smallint`, `int`, `bigint`) for optimal storage and join performance.

---

## Consolidated Employee and User Identity

Per approved rule: **ONE EMPLOYEE = ONE DO.OneAccess USER**.

The identity model clearly distinguishes three concepts:
1. **Obsolete Separate Employee Identity (`EmployeeId` / `Employees` table)**:
   - In prior architectures, employees were stored in a separate `Employees` table with an `EmployeeId`.
   - This separate domain and technical entity has been permanently dropped and consolidated directly into `Users`.
   - `EmployeeId` no longer exists as a technical identity or column in DO.OneAccess.

2. **Current Technical User Identity (`UserId`)**:
   - `UserId` (`Guid` / `uniqueidentifier`, Primary Key) is the single, stable, immutable technical identity for the employee user account.
   - Used for authentication, JWT subject (`sub`), authorization, role assignment, audit logging, and external system reference.

3. **Current Business Employee Identifier (`EmployeeNumber`)**:
   - `EmployeeNumber` (`string` / `nvarchar(50)`, Unique Key, `IsRequired()`) is the official organization-defined, HR-managed employee identifier.
   - Assigned and maintained by HR to reflect organizational staffing records, stored directly on the `Users` entity (`Users.EmployeeNumber`).

---

## Future Identity Use Case

When a future office system (e.g., a payroll system) needs to reference a DO.OneAccess user:

```
Payroll System record
  └── doOneAccessUserId: "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

This reference is stable even if the user's name, email, or EmployeeNumber changes.

---

## Related Documents

| Document | Link |
|---|---|
| Integration Overview | [Overview.md](Overview.md) |
| Integration Contract | [Integration-Contract.md](Integration-Contract.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
| Roles | [../Access-Control/Roles.md](../Access-Control/Roles.md) |
