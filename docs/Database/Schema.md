# Database — Schema

> **Section:** Database  
> **Document:** Schema  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Tables.md](Tables.md) | [Relationships.md](Relationships.md) | [Indexes.md](Indexes.md) | [../System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## Database Engine

**SQL Server** (configured via `appsettings.json` / environment secrets).

---

## Schema Design Principles

| Principle | Application |
|---|---|
| Tiered Identifier Strategy | GUIDs for integration identities (`UserId`, `SystemId`), with `EmployeeNumber` for HR business identity; numeric for internal reference (`RoleId`, `DivisionId`, `SectionId`); `bigint` for high-volume logs and access records |
| Explicit PK Naming | Entity-specific primary key names (`<Entity>Id`), never generic `Id` |
| FK constraints | All foreign keys enforced at the database level |
| Unique constraints | Applied wherever data uniqueness is required (`EmployeeNumber`, `Username`, `UNIQUE(DivisionId, Code)`, `UNIQUE(AdministratorUserId, SystemId)`) |
| Soft deletion | Core entities use `IsActive` flag |
| Standard Audit Fields | Mutable entities track `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` |
| No permissions tables | No over-engineered permission/claim tables |
| No generic AppSettings table | Only `SystemSettings` for system-specific business config (`SettingKey`, `SettingValue`) |

---

## Common Column Patterns

### Standard Entity Columns
All mutable business entities include:

| Column | Type | Notes |
|---|---|---|
| `<Entity>Id` | Entity-specific (`uniqueidentifier` / `int` / `bigint` / `smallint`) | Explicit entity PK |
| `CreatedAt` | `datetime2` NOT NULL | UTC timestamp set on insert |
| `CreatedBy` | `uniqueidentifier` NULL | Actor `UserId` who created the record |
| `UpdatedAt` | `datetime2` NULL | UTC timestamp updated on each modification |
| `UpdatedBy` | `uniqueidentifier` NULL | Actor `UserId` who last modified the record |

### Soft Delete
Entities that use soft delete or activation state include:

| Column | Type | Notes |
|---|---|---|
| `IsActive` | `bit` NOT NULL DEFAULT 1 | `1` = active, `0` = deactivated |

### Audit Reference
Standard `CreatedBy` and `UpdatedBy` fields reference `Users.UserId`. Where a business action specifically requires an explicit actor relationship, an explicit User FK is used. Redundant actor tracking fields are avoided.

---

## Table List (12 Canonical Tables)

| # | Table | Primary Key | Key Columns / Constraints | Description |
|---|---|---|---|---|
| 1 | `Roles` | `RoleId` (`smallint`) | `Code` (UK), `Name`, `Description`, `IsActive` | Role reference data |
| 2 | `Divisions` | `DivisionId` (`int`) | `Code` (UK), `Name`, `Description`, `IsActive` | Organizational divisions |
| 3 | `Sections` | `SectionId` (`int`) | `DivisionId` (FK), `Code`, `Name`, `Description`, `IsActive`, `UNIQUE(DivisionId, Code)` | Sections within Divisions |
| 4 | `Users` | `UserId` (`uniqueidentifier`) | `EmployeeNumber` (UK), `FirstName`, `MiddleName` (null), `LastName`, `Email`, `Position` (null), `SectionId` (FK, null), `RoleId` (FK), `Username` (UK), `PasswordHash`, `IsActive`, `LastLoginAt` | Consolidated employee profile & user account |
| 5 | `Systems` | `SystemId` (`uniqueidentifier`) | `SystemCode` (UK), `SystemName`, `Description`, `BaseUrl`, `IconUrl`, `DefaultAccess`, `IsActive` | Registered office systems |
| 6 | `SystemSettings` | `SystemSettingId` (`bigint`) | `SystemId` (FK), `SettingKey`, `SettingValue`, `IsSecret`, `UNIQUE(SystemId, SettingKey)` | Per-system key-value configuration |
| 7 | `UserSystemAccess` | `UserSystemAccessId` (`bigint`) | `UserId` (FK), `SystemId` (FK), `AccessType`, `UNIQUE(UserId, SystemId)` | Explicit user/system access overrides |
| 8 | `AdministratorScopes` | `AdministratorScopeId` (`bigint`) | `AdministratorUserId` (FK, UK), `DivisionId` (FK) | Administrator-to-Division management scope |
| 9 | `AdministratorSystemAccess` | `AdministratorSystemAccessId` (`bigint`) | `AdministratorUserId` (FK), `SystemId` (FK), `UNIQUE(AdministratorUserId, SystemId)` | Administrator-to-System management grants |
| 10 | `RefreshTokens` | `RefreshTokenId` (`bigint`) | `UserId` (FK), `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenId`, `CreatedByIp`, `RevokedByIp` | Hashed refresh token store |
| 11 | `LoginHistories` | `LoginHistoryId` (`bigint`) | `UserId` (FK, nullable), `UsernameAttempted`, `Success`, `IpAddress`, `UserAgent`, `FailureReason`, `CreatedAt` | Login event records |
| 12 | `AuditLogs` | `AuditLogId` (`bigint`) | `UserId` (FK, nullable), `Action`, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `IpAddress`, `CreatedAt` | Administrative and access audit trail |

Full column details: [Tables.md](Tables.md)  
Full relationship details: [Relationships.md](Relationships.md)  
Index definitions: [Indexes.md](Indexes.md)

---

## Access Concept Separation (Schema)

> `UserSystemAccess` — per-user system **usage** access overrides (`AccessType`: `Allow` / `Deny`).  
> `AdministratorSystemAccess` — Administrator **system-management** grants (separate from UserSystemAccess).  
> `AdministratorScopes` — Administrator **management** scope (Division assignment).  
> These are distinct tables with distinct purposes. Do not conflate.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Tables | [Tables.md](Tables.md) |
| Relationships | [Relationships.md](Relationships.md) |
| Indexes | [Indexes.md](Indexes.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
| Database Architecture | [../System-Architecture/Database-Architecture.md](../System-Architecture/Database-Architecture.md) |
| Seed Data | [Seed-Data.md](Seed-Data.md) |
| Migration Strategy | [Migration-Strategy.md](Migration-Strategy.md) |
