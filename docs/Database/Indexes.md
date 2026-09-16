# Database — Indexes

> **Section:** Database  
> **Document:** Indexes  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Tables.md](Tables.md) | [Schema.md](Schema.md) | [Relationships.md](Relationships.md)

---

## Indexing Philosophy

Indexes are applied to:
1. **All foreign key columns** — SQL Server does not automatically index FKs; indexes are required for join performance.
2. **Frequently queried non-PK columns** — Columns that appear in `WHERE` clauses in common queries.
3. **Unique constraints** — SQL Server creates a unique index automatically for `UNIQUE` constraints.

Indexes are not over-applied. Only add indexes that address real query patterns.

---

## Primary Key Indexes

SQL Server automatically creates a **clustered index** on each table's explicit entity primary key:

| Table | PK Column | Data Type | Clustered Index |
|---|---|---|---|
| `Roles` | `RoleId` | `smallint` | PK clustered index |
| `Divisions` | `DivisionId` | `int` | PK clustered index |
| `Sections` | `SectionId` | `int` | PK clustered index |
| `Users` | `UserId` | `uniqueidentifier` | PK clustered index |
| `Systems` | `SystemId` | `uniqueidentifier` | PK clustered index |
| `SystemSettings` | `SystemSettingId` | `bigint` | PK clustered index |
| `UserSystemAccess` | `UserSystemAccessId` | `bigint` | PK clustered index |
| `AdministratorScopes` | `AdministratorScopeId` | `bigint` | PK clustered index |
| `AdministratorSystemAccess` | `AdministratorSystemAccessId` | `bigint` | PK clustered index |
| `RefreshTokens` | `RefreshTokenId` | `bigint` | PK clustered index |
| `LoginHistories` | `LoginHistoryId` | `bigint` | PK clustered index |
| `AuditLogs` | `AuditLogId` | `bigint` | PK clustered index |

---

## Unique Constraint Indexes (Auto-created)

| Table | Column(s) | Type | Notes |
|---|---|---|---|
| `Roles` | `Code` | Unique non-clustered | Role programmatic code |
| `Divisions` | `Code` | Unique non-clustered | Division short code |
| `Sections` | `(DivisionId, Code)` | Unique non-clustered (composite) | Unique within parent division |
| `Users` | `EmployeeNumber` | Unique non-clustered | Employee business identifier |
| `Users` | `Username` | Unique non-clustered | Unique login identifier |
| `Systems` | `SystemCode` | Unique non-clustered | Canonical system code |
| `SystemSettings` | `(SystemId, SettingKey)` | Unique non-clustered (composite) | Unique setting key per system |
| `UserSystemAccess` | `(UserId, SystemId)` | Unique non-clustered (composite) | Explicit access rule per user/system |
| `AdministratorScopes` | `AdministratorUserId` | Unique non-clustered | One division scope per admin |
| `AdministratorSystemAccess` | `(AdministratorUserId, SystemId)` | Unique non-clustered (composite) | One management grant per admin/system |

---

## Foreign Key Indexes (Explicit)

| Table | Column | Purpose |
|---|---|---|
| `Sections` | `DivisionId` | Join Sections to Division |
| `Users` | `SectionId` | Join Users to Section (nullable) |
| `Users` | `RoleId` | Join Users to Role |
| `SystemSettings` | `SystemId` | Join SystemSettings to System |
| `UserSystemAccess` | `UserId` | Lookup access rules by User |
| `UserSystemAccess` | `SystemId` | Lookup access rules by System |
| `AdministratorScopes` | `DivisionId` | Lookup scopes by Division |
| `AdministratorSystemAccess` | `AdministratorUserId` | Lookup system grants by Administrator |
| `AdministratorSystemAccess` | `SystemId` | Lookup Administrators assigned to System |
| `RefreshTokens` | `UserId` | Lookup tokens by User |
| `RefreshTokens` | `ReplacedByTokenId` | Lookup replacement token during rotation |
| `LoginHistories` | `UserId` | Lookup history by User |
| `AuditLogs` | `UserId` | Lookup audit entries by actor |

---

## Query-Pattern Indexes

| Table | Column | Query Pattern |
|---|---|---|
| `RefreshTokens` | `TokenHash` | Lookup token by cryptographic hash on refresh |
| `RefreshTokens` | `RevokedAt`, `ExpiresAt` | Filter active tokens |
| `LoginHistories` | `CreatedAt` | Date-range queries |
| `LoginHistories` | `UsernameAttempted` | Filter login attempts by username |
| `AuditLogs` | `CreatedAt` | Date-range queries |
| `AuditLogs` | `EntityName`, `EntityId` | Lookup audit entries by entity |
| `Users` | `IsActive` | Filter active users |
| `Systems` | `IsActive` | Filter active systems |

---

## Notes

- For GUID primary keys (`UserId`, `SystemId`), consider using sequential GUID generation (`newsequentialid()` in SQL Server or equivalent application generator) to minimize page fragmentation on clustered indexes.
- Index definitions will be formalized in EF Core entity configurations (`IEntityTypeConfiguration<T>`) during implementation.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Tables | [Tables.md](Tables.md) |
| Schema | [Schema.md](Schema.md) |
| Relationships | [Relationships.md](Relationships.md) |
