# System Architecture — Database Architecture

> **Section:** System Architecture  
> **Document:** Database Architecture  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [ERD.md](ERD.md) | [../Database/Schema.md](../Database/Schema.md) | [../Database/Tables.md](../Database/Tables.md)

---

## Database Engine

**SQL Server** is the approved database engine for DO.OneAccess.

---

## Design Principles

| Principle | Application |
|---|---|
| Tiered Identifier Strategy | GUIDs for integration identities (`UserId`, `SystemId`); integers for reference entities (`RoleId`, `DivisionId`, `SectionId`); `bigint` for high-volume logs/access |
| Explicit PK Naming | Entity-specific primary keys (`<Entity>Id`), never generic `Id` |
| Referential integrity | All foreign key relationships enforced at the database level |
| Unique constraints | Applied where data must be unique (e.g., `EmployeeNumber`, `Username`, `UNIQUE(DivisionId, Code)`) |
| Soft deletion | Core business entities use `IsActive` flag; not immediate hard deletion |
| No permission tables | No over-engineered permission/claim tables; roles and explicit access records are sufficient |
| No generic settings table | `SystemSettings` is for system-specific business config only (`SettingKey`, `SettingValue`) |
| Indexes | Applied to foreign keys, unique constraints, and frequently queried columns |

---

## Table Inventory (12 Canonical Tables)

| # | Table | PK Column & Type | Purpose |
|---|---|---|---|
| 1 | `Roles` | `RoleId` (`smallint`) | Role definitions (SYSTEM_ADMINISTRATOR, ADMINISTRATOR, USER) |
| 2 | `Divisions` | `DivisionId` (`int`) | Organizational divisions |
| 3 | `Sections` | `SectionId` (`int`) | Sections within a Division |
| 4 | `Users` | `UserId` (`uniqueidentifier`) | Consolidated employee profile and user account |
| 5 | `Systems` | `SystemId` (`uniqueidentifier`) | Registered office systems |
| 6 | `SystemSettings` | `SystemSettingId` (`bigint`) | Per-system business configuration key-value pairs |
| 7 | `UserSystemAccess` | `UserSystemAccessId` (`bigint`) | Explicit per-user system access override (Allow/Deny) |
| 8 | `AdministratorScopes` | `AdministratorScopeId` (`bigint`) | Administrator-to-Division management scope grants |
| 9 | `AdministratorSystemAccess` | `AdministratorSystemAccessId` (`bigint`) | Administrator-to-System management grants (separate from UserSystemAccess) |
| 10 | `RefreshTokens` | `RefreshTokenId` (`bigint`) | Server-side hashed refresh token store |
| 11 | `LoginHistories` | `LoginHistoryId` (`bigint`) | Login event records per user |
| 12 | `AuditLogs` | `AuditLogId` (`bigint`) | Audit trail for administrative and access actions |

---

## Core Relationship Summary

```
Division --1:N--> Section --1:N (nullable)--> User --N:1--> Role
                                                |
                               +----------------+
                               |                |
                               v                v
                         UserSystemAccess RefreshTokens
                               |          LoginHistories
                               v          AuditLogs
                            Systems
                              |
                              v
                        SystemSettings

Administrator (User) --1:0..1--> AdministratorScope --N:1--> Division
Administrator (User) --1:N-----> AdministratorSystemAccess --N:1--> System
```

---

## Key Constraints

| Constraint | Table | Columns | Notes |
|---|---|---|---|
| Unique | `Roles` | `Code` | Role programmatic code |
| Unique | `Divisions` | `Code` | Division code |
| Unique | `Sections` | `(DivisionId, Code)` | Scoped within Division |
| Unique | `Users` | `EmployeeNumber` | Employee business identifier |
| Unique | `Users` | `Username` | Login identifier |
| Unique | `Systems` | `SystemCode` | Canonical system code |
| Unique | `SystemSettings` | `(SystemId, SettingKey)` | Unique setting key per system |
| Unique | `UserSystemAccess` | `(UserId, SystemId)` | Explicit rule per user/system |
| Unique | `AdministratorScopes` | `AdministratorUserId` | One division scope per Administrator |
| Unique | `AdministratorSystemAccess` | `(AdministratorUserId, SystemId)` | System management grant |
| FK | `Sections` | `DivisionId → Divisions.DivisionId` | |
| FK | `Users` | `SectionId → Sections.SectionId` | Nullable |
| FK | `Users` | `RoleId → Roles.RoleId` | |
| FK | `UserSystemAccess` | `UserId → Users.UserId` | |
| FK | `UserSystemAccess` | `SystemId → Systems.SystemId` | |
| FK | `AdministratorScopes` | `AdministratorUserId → Users.UserId` | |
| FK | `AdministratorScopes` | `DivisionId → Divisions.DivisionId` | |
| FK | `AdministratorSystemAccess` | `AdministratorUserId → Users.UserId` | |
| FK | `AdministratorSystemAccess` | `SystemId → Systems.SystemId` | |
| FK | `RefreshTokens` | `UserId → Users.UserId` | |
| FK | `LoginHistories` | `UserId → Users.UserId` | Nullable |
| FK | `AuditLogs` | `UserId → Users.UserId` | Nullable |
| FK | `SystemSettings` | `SystemId → Systems.SystemId` | |

---

## Access Model Separation

> **Important:** `UserSystemAccess` governs whether a User can **use** a System (`AccessType`: `Allow` / `Deny`).  
> `AdministratorSystemAccess` governs which Systems an Administrator can **manage**. This is separate from `UserSystemAccess`.  
> `AdministratorScopes` governs which Division an Administrator can **manage**.  
> These are entirely separate concepts and must not be conflated.

---

## Soft Deletion Policy

The following tables include `IsActive` for soft deletion or activation state:

- `Roles`
- `Divisions`
- `Sections`
- `Users`
- `Systems`

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| ERD | [ERD.md](ERD.md) |
| Tables Detail | [../Database/Tables.md](../Database/Tables.md) |
| Schema | [../Database/Schema.md](../Database/Schema.md) |
| Relationships | [../Database/Relationships.md](../Database/Relationships.md) |
| Indexes | [../Database/Indexes.md](../Database/Indexes.md) |
