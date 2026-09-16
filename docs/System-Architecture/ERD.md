# System Architecture — ERD

> **Section:** System Architecture  
> **Document:** Entity Relationship Diagram  
> **Syntax:** Mermaid ERD  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)

---

## Entity Relationship Diagram

```mermaid
erDiagram
    Roles {
        smallint RoleId PK
        string Code UK
        string Name
        string Description
        bool IsActive
    }

    Divisions {
        int DivisionId PK
        string Code UK
        string Name
        string Description
        bool IsActive
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    Sections {
        int SectionId PK
        int DivisionId FK "UK composite"
        string Code "UK composite"
        string Name
        string Description
        bool IsActive
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    Users {
        uniqueidentifier UserId PK
        string EmployeeNumber UK
        string FirstName
        string MiddleName
        string LastName
        string Email
        string Position
        int SectionId FK
        smallint RoleId FK
        string Username UK
        string PasswordHash
        bool IsActive
        datetime LastLoginAt
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    Systems {
        uniqueidentifier SystemId PK
        string SystemCode UK
        string SystemName
        string Description
        string BaseUrl
        string IconUrl
        string DefaultAccess
        bool IsActive
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    SystemSettings {
        bigint SystemSettingId PK
        uniqueidentifier SystemId FK "UK composite"
        string SettingKey "UK composite"
        string SettingValue
        bool IsSecret
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    UserSystemAccess {
        bigint UserSystemAccessId PK
        uniqueidentifier UserId FK "UK composite"
        uniqueidentifier SystemId FK "UK composite"
        string AccessType
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    AdministratorScopes {
        bigint AdministratorScopeId PK
        uniqueidentifier AdministratorUserId FK "UK"
        int DivisionId FK
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    AdministratorSystemAccess {
        bigint AdministratorSystemAccessId PK
        uniqueidentifier AdministratorUserId FK "UK composite"
        uniqueidentifier SystemId FK "UK composite"
        datetime CreatedAt
        uniqueidentifier CreatedBy
        datetime UpdatedAt
        uniqueidentifier UpdatedBy
    }

    RefreshTokens {
        bigint RefreshTokenId PK
        uniqueidentifier UserId FK
        string TokenHash
        datetime ExpiresAt
        datetime CreatedAt
        datetime RevokedAt
        bigint ReplacedByTokenId
        string CreatedByIp
        string RevokedByIp
    }

    LoginHistories {
        bigint LoginHistoryId PK
        uniqueidentifier UserId FK
        string UsernameAttempted
        bool Success
        string IpAddress
        string UserAgent
        string FailureReason
        datetime CreatedAt
    }

    AuditLogs {
        bigint AuditLogId PK
        uniqueidentifier UserId FK
        string Action
        string EntityName
        string EntityId
        string OldValues
        string NewValues
        string IpAddress
        datetime CreatedAt
    }

    Divisions ||--o{ Sections : "contains"
    Sections ||--o{ Users : "contains (nullable for sysadmin/admin)"
    Roles ||--o{ Users : "assigned to"
    Users ||--o{ UserSystemAccess : "has access rules"
    Systems ||--o{ UserSystemAccess : "subject of"
    Systems ||--o{ SystemSettings : "has settings"
    Users ||--o{ AdministratorScopes : "scoped to"
    Divisions ||--o{ AdministratorScopes : "managed by"
    Users ||--o{ AdministratorSystemAccess : "manages systems"
    Systems ||--o{ AdministratorSystemAccess : "managed by admin"
    Users ||--o{ RefreshTokens : "holds"
    Users ||--o{ LoginHistories : "recorded for"
    Users ||--o{ AuditLogs : "performed by"
```

---

## Cardinality Summary

| Relationship | Cardinality | Notes |
|---|---|---|
| Division → Section | 1 : N | A Division has many Sections |
| Section → User | 1 : N | A Section contains Users (nullable for SysAdmin/Admin) |
| Role → User | 1 : N | Each User has exactly one Role |
| User → UserSystemAccess | 1 : N | A User may have multiple access overrides |
| System → UserSystemAccess | 1 : N | A System may have many user overrides |
| System → SystemSettings | 1 : N | A System has multiple setting keys |
| User → AdministratorScope | 1 : 0..1 | Administrators have at most one scope |
| Division → AdministratorScope | 1 : N | A Division may be the scope of multiple Admins |
| User → AdministratorSystemAccess | 1 : N | An Administrator may be granted management rights over multiple systems |
| System → AdministratorSystemAccess | 1 : N | A System may be granted to multiple Administrators |
| User → RefreshTokens | 1 : N | A User may have multiple refresh tokens (rotation/multi-device) |
| User → LoginHistories | 1 : N | Many login events per user (UserId nullable for unresolved attempts) |
| User → AuditLogs | 1 : N | Audit log entries (UserId nullable for system events) |

---

## Key Constraints

| Constraint | Entity | Field(s) | Notes |
|---|---|---|---|
| Unique | `Roles` | `Code` | Programmatic role code |
| Unique | `Divisions` | `Code` | Unique division code |
| Unique | `Sections` | `(DivisionId, Code)` | Unique within Division |
| Unique | `Users` | `EmployeeNumber` | Official employee business identifier |
| Unique | `Users` | `Username` | Login identifier |
| Unique | `Systems` | `SystemCode` | Canonical system code |
| Unique | `SystemSettings` | `(SystemId, SettingKey)` | Unique setting key per system |
| Unique | `UserSystemAccess` | `(UserId, SystemId)` | One rule per user per system |
| Unique | `AdministratorScopes` | `AdministratorUserId` | One division scope per Administrator |
| Unique | `AdministratorSystemAccess` | `(AdministratorUserId, SystemId)` | One management grant per admin per system |

---

## Access Concept Separation

> `UserSystemAccess` — controls whether a **User can use** a System (`AccessType`: `Allow` / `Deny`).  
> `AdministratorSystemAccess` — controls which **Systems an Administrator can manage**. Separate from `UserSystemAccess`.  
> `AdministratorScopes` — controls which **Division an Administrator can manage**.  
> These are distinct concepts and must not be conflated.

---

## Notes

- **Identifier Strategy:**
  - `uniqueidentifier` / GUID: Externally meaningful and integration-facing identities (`UserId`, `SystemId`). `UserId` is the single stable technical identity.
  - `smallint` / `int`: Internal reference entities (`RoleId`, `DivisionId`, `SectionId`).
  - `bigint`: High-volume transactional, history, and access records (`AdministratorScopeId`, `AdministratorSystemAccessId`, `UserSystemAccessId`, `RefreshTokenId`, `LoginHistoryId`, `AuditLogId`, `SystemSettingId`).
- **Primary Key Naming:** Explicit entity-specific `<Entity>Id` naming is canonical; generic `Id` is not used.
- `AuditLogs` must never contain passwords, tokens, or secrets. `UserId` is nullable for system/background events.
- `RefreshTokens.TokenHash` stores a cryptographic hash, never plaintext. Revocation tracked via `RevokedAt` timestamp and `ReplacedByTokenId`.
- `AdministratorSystemAccess.AdministratorUserId` references an Administrator User. System Administrator does not require an entry (global authority).
- `docs/DO.OneAccess-Specification-v1.md` is the canonical source of truth for architecture and database definitions.
