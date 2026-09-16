# Database — Tables

> **Section:** Database  
> **Document:** Tables — Column Definitions  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Schema.md](Schema.md) | [Relationships.md](Relationships.md) | [../System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## 1. Roles

| Column | Type | Constraints | Description |
|---|---|---|---|
| `RoleId` | `smallint` | PK, NOT NULL | Role primary key (1, 2, 3) |
| `Code` | `nvarchar(50)` | NOT NULL, UNIQUE | Role programmatic code (`SYSTEM_ADMINISTRATOR`, `ADMINISTRATOR`, `USER`) |
| `Name` | `nvarchar(100)` | NOT NULL | Human-readable role name (`System Administrator`, `Administrator`, `User`) |
| `Description` | `nvarchar(200)` | NULL | Description of authority |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Role active status |

**Notes:** Seeded at initialization with exactly three roles. `Code` and `Name` are distinct and not interchangeable. Role records must not be deleted.

---

## 2. Divisions

| Column | Type | Constraints | Description |
|---|---|---|---|
| `DivisionId` | `int` | PK, NOT NULL, IDENTITY | Division primary key |
| `Code` | `nvarchar(20)` | NOT NULL, UNIQUE | Short unique division code |
| `Name` | `nvarchar(100)` | NOT NULL | Division name |
| `Description` | `nvarchar(500)` | NULL | Detailed description |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft delete |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

---

## 3. Sections

| Column | Type | Constraints | Description |
|---|---|---|---|
| `SectionId` | `int` | PK, NOT NULL, IDENTITY | Section primary key |
| `DivisionId` | `int` | FK → Divisions.DivisionId, NOT NULL | Parent Division |
| `Code` | `nvarchar(20)` | NOT NULL | Section short code |
| `Name` | `nvarchar(100)` | NOT NULL | Section name |
| `Description` | `nvarchar(500)` | NULL | Detailed description |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft delete |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Unique constraint:** `UNIQUE(DivisionId, Code)` — Section Code is unique within its parent Division, not globally unique.

---

## 4. Users

Consolidated entity containing official employee profile details and user account credentials per approved rule: **ONE EMPLOYEE = ONE DO.OneAccess USER**.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `UserId` | `uniqueidentifier` | PK, NOT NULL | Stable identity GUID; single technical identity across platform |
| `EmployeeNumber` | `nvarchar(50)` | NOT NULL, UNIQUE | Official employee/business identifier |
| `FirstName` | `nvarchar(100)` | NOT NULL | First name |
| `MiddleName` | `nvarchar(100)` | NULL | Middle name (optional) |
| `LastName` | `nvarchar(100)` | NOT NULL | Last name |
| `Email` | `nvarchar(200)` | NOT NULL | Official contact/identity email address |
| `Position` | `nvarchar(100)` | NULL | Official job position (optional) |
| `SectionId` | `int` | FK → Sections.SectionId, NULL | Section assignment (NULL for SysAdmin; optional for Admin; required for User) |
| `RoleId` | `smallint` | FK → Roles.RoleId, NOT NULL | User role (1 = SysAdmin, 2 = Admin, 3 = User) |
| `Username` | `nvarchar(50)` | NOT NULL, UNIQUE | Unique login identifier |
| `PasswordHash` | `nvarchar(500)` | NOT NULL | Hashed password; never plaintext |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft delete / account active flag |
| `LastLoginAt` | `datetime2` | NULL | Last successful login timestamp |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Identity Rules:**
- `UserId` is the single stable technical identity for authentication, authorization, scopes, audit, and external references.
- `Username` is the unique login identifier.
- `EmployeeNumber` is the official employee/business identifier.
- `Email` is employee contact/identity information.
- `SectionId` is nullable at database level:
  - `SYSTEM_ADMINISTRATOR`: `SectionId = NULL` (global scope).
  - `ADMINISTRATOR`: `SectionId` may be `NULL` (administrative authority derives from `AdministratorScopes.DivisionId`).
  - `USER`: `SectionId` is required (belongs to a Section under a Division).
- Do not use `EmployeeNumber` or `Email` as the `Users` primary key.

---

## 5. Systems

| Column | Type | Constraints | Description |
|---|---|---|---|
| `SystemId` | `uniqueidentifier` | PK, NOT NULL | Stable system identity GUID; referenced by integrations |
| `SystemCode` | `nvarchar(20)` | NOT NULL, UNIQUE | Canonical short code (e.g., `HR`, `PAYROLL`) |
| `SystemName` | `nvarchar(100)` | NOT NULL | Canonical system display name |
| `Description` | `nvarchar(500)` | NULL | Description of the system |
| `BaseUrl` | `nvarchar(500)` | NULL | URL to link to the system |
| `IconUrl` | `nvarchar(500)` | NULL | System icon URL |
| `DefaultAccess` | `nvarchar(20)` | NOT NULL | `All` or `Restricted` |
| `IsActive` | `bit` | NOT NULL, DEFAULT 1 | Soft delete / active flag |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

---

## 6. SystemSettings

| Column | Type | Constraints | Description |
|---|---|---|---|
| `SystemSettingId` | `bigint` | PK, NOT NULL, IDENTITY | Setting primary key |
| `SystemId` | `uniqueidentifier` | FK → Systems.SystemId, NOT NULL | Parent system |
| `SettingKey` | `nvarchar(100)` | NOT NULL | Canonical setting key |
| `SettingValue` | `nvarchar(max)` | NULL | Canonical setting value |
| `IsSecret` | `bit` | NOT NULL, DEFAULT 0 | Secret flag (masked in UI/audit) |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Unique constraint:** `UNIQUE(SystemId, SettingKey)`

---

## 7. UserSystemAccess

| Column | Type | Constraints | Description |
|---|---|---|---|
| `UserSystemAccessId` | `bigint` | PK, NOT NULL, IDENTITY | Override rule primary key |
| `UserId` | `uniqueidentifier` | FK → Users.UserId, NOT NULL | Target user |
| `SystemId` | `uniqueidentifier` | FK → Systems.SystemId, NOT NULL | Target system |
| `AccessType` | `nvarchar(20)` | NOT NULL | `Allow` or `Deny` |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Creator user identity |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Unique constraint:** `UNIQUE(UserId, SystemId)`

**Access resolution:**
1. Explicit `UserSystemAccess` overrides `System.DefaultAccess`.
2. If no explicit rule exists, fallback to `System.DefaultAccess`.

---

## 8. AdministratorScopes

| Column | Type | Constraints | Description |
|---|---|---|---|
| `AdministratorScopeId` | `bigint` | PK, NOT NULL, IDENTITY | Scope primary key |
| `AdministratorUserId` | `uniqueidentifier` | FK → Users.UserId, NOT NULL, UNIQUE | Administrator's UserId |
| `DivisionId` | `int` | FK → Divisions.DivisionId, NOT NULL | Assigned Division |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | System Admin who granted scope |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Unique constraint:** `AdministratorUserId` (one division scope per Administrator)

---

## 9. AdministratorSystemAccess

Records which Systems a specific Administrator is authorized to manage. Granted exclusively by the System Administrator.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `AdministratorSystemAccessId` | `bigint` | PK, NOT NULL, IDENTITY | Management grant primary key |
| `AdministratorUserId` | `uniqueidentifier` | FK → Users.UserId, NOT NULL | Administrator's UserId; must have Administrator role |
| `SystemId` | `uniqueidentifier` | FK → Systems.SystemId, NOT NULL | System the Administrator may manage |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `CreatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | System Admin who granted management rights |
| `UpdatedAt` | `datetime2` | NULL | Last modification timestamp |
| `UpdatedBy` | `uniqueidentifier` | NULL, FK → Users.UserId | Modifier user identity |

**Unique constraint:** `UNIQUE(AdministratorUserId, SystemId)`

> **Note:** Purpose is defining which Systems an Administrator may manage. It is NOT `UserSystemAccess`. System Administrator does not require an `AdministratorSystemAccess` record (global authority).

---

## 10. RefreshTokens

| Column | Type | Constraints | Description |
|---|---|---|---|
| `RefreshTokenId` | `bigint` | PK, NOT NULL, IDENTITY | Token primary key |
| `UserId` | `uniqueidentifier` | FK → Users.UserId, NOT NULL | Token owner |
| `TokenHash` | `nvarchar(500)` | NOT NULL | Cryptographic hash of refresh token; never plaintext |
| `ExpiresAt` | `datetime2` | NOT NULL | Token expiry timestamp |
| `CreatedAt` | `datetime2` | NOT NULL | Record creation timestamp |
| `RevokedAt` | `datetime2` | NULL | Timestamp of revocation |
| `ReplacedByTokenId` | `bigint` | NULL, FK → RefreshTokens.RefreshTokenId | Rotation link to replacement token |
| `CreatedByIp` | `nvarchar(50)` | NULL | IP address of requester at issuance |
| `RevokedByIp` | `nvarchar(50)` | NULL | IP address of requester at revocation |

**Notes:** Never store raw refresh tokens. Do not replace this model with only `IsRevoked`.

---

## 11. LoginHistories

| Column | Type | Constraints | Description |
|---|---|---|---|
| `LoginHistoryId` | `bigint` | PK, NOT NULL, IDENTITY | Login event primary key |
| `UserId` | `uniqueidentifier` | FK → Users.UserId, NULL | Null if user not resolved |
| `UsernameAttempted` | `nvarchar(100)` | NULL | Username supplied during login attempt |
| `Success` | `bit` | NOT NULL | Login success flag |
| `IpAddress` | `nvarchar(50)` | NULL | Requester IP address |
| `UserAgent` | `nvarchar(500)` | NULL | Requester user agent |
| `FailureReason` | `nvarchar(200)` | NULL | Generic failure reason; no passwords |
| `CreatedAt` | `datetime2` | NOT NULL | Event timestamp |

---

## 12. AuditLogs

| Column | Type | Constraints | Description |
|---|---|---|---|
| `AuditLogId` | `bigint` | PK, NOT NULL, IDENTITY | Audit log primary key |
| `UserId` | `uniqueidentifier` | FK → Users.UserId, NULL | Actor UserId (null for system/background/security events) |
| `Action` | `nvarchar(100)` | NOT NULL | Action name (e.g., `CreateUser`, `GrantSystemAccess`) |
| `EntityName` | `nvarchar(100)` | NULL | Affected entity name (e.g., `User`, `System`) |
| `EntityId` | `nvarchar(100)` | NULL | Affected entity identifier |
| `OldValues` | `nvarchar(max)` | NULL | Serialized old state; no secrets |
| `NewValues` | `nvarchar(max)` | NULL | Serialized new state; no secrets |
| `IpAddress` | `nvarchar(50)` | NULL | Actor IP address |
| `CreatedAt` | `datetime2` | NOT NULL | Event timestamp |

> **Audit Security:** `AuditLogs` must not require a valid `UserId` because system/background/security events may not always have an authenticated user. Never store passwords, tokens, or secrets in audit payloads.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Schema | [Schema.md](Schema.md) |
| Relationships | [Relationships.md](Relationships.md) |
| Indexes | [Indexes.md](Indexes.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
