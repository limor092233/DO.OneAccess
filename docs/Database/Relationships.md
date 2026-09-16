# Database — Relationships

> **Section:** Database  
> **Document:** Relationships  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Tables.md](Tables.md) | [Schema.md](Schema.md) | [../System-Architecture/ERD.md](../System-Architecture/ERD.md)

---

## Relationship Summary

| Relationship | Cardinality | FK Column | Referenced Table & Column | Notes |
|---|---|---|---|---|
| Division → Section | 1 : N | `Sections.DivisionId` | `Divisions.DivisionId` | Parent division |
| Section → User | 1 : N | `Users.SectionId` | `Sections.SectionId` | Section assignment (`NULL` for SysAdmin/Admin) |
| Role → User | 1 : N | `Users.RoleId` | `Roles.RoleId` | Role assignment |
| System → SystemSetting | 1 : N | `SystemSettings.SystemId` | `Systems.SystemId` | System settings |
| User → UserSystemAccess | 1 : N | `UserSystemAccess.UserId` | `Users.UserId` | User access rule |
| System → UserSystemAccess | 1 : N | `UserSystemAccess.SystemId` | `Systems.SystemId` | System access rule |
| User → AdministratorScope | 1 : 0..1 | `AdministratorScopes.AdministratorUserId` | `Users.UserId` | Admin division scope (`UNIQUE`) |
| Division → AdministratorScope | 1 : N | `AdministratorScopes.DivisionId` | `Divisions.DivisionId` | Division under management |
| User → AdministratorSystemAccess | 1 : N | `AdministratorSystemAccess.AdministratorUserId` | `Users.UserId` | Admin system grant |
| System → AdministratorSystemAccess | 1 : N | `AdministratorSystemAccess.SystemId` | `Systems.SystemId` | System under management |
| User → RefreshToken | 1 : N | `RefreshTokens.UserId` | `Users.UserId` | Refresh token owner |
| User → LoginHistory | 1 : N | `LoginHistories.UserId` | `Users.UserId` | Login attempt (nullable) |
| User → AuditLog | 1 : N | `AuditLogs.UserId` | `Users.UserId` | Audit trail actor (nullable) |

---

## Standard Audit Actor References

All mutable business entities include `CreatedBy` and `UpdatedBy` columns which reference `Users.UserId` (`uniqueidentifier`, nullable):

- `Divisions`
- `Sections`
- `Users`
- `Systems`
- `SystemSettings`
- `UserSystemAccess`
- `AdministratorScopes`
- `AdministratorSystemAccess`

Where a specific business action requires actor tracking (such as recording which System Administrator granted an Administrator scope or system access), standard `CreatedBy` / `UpdatedBy` are utilized. Redundant custom actor columns (e.g., `GrantedByUserId`) are not created.

---

## Cascade Behavior

> **Policy:** Cascading deletes are not used. Referential integrity is maintained via soft-delete (`IsActive`) and application-level validation. Hard deletes of parent records with existing child references are prevented by database foreign key constraints.

| Relationship | On Delete Behavior |
|---|---|
| Division → Section | Restrict / No cascade; deactivate children in application |
| Section → User | Restrict / No cascade; deactivate in application |
| User → RefreshTokens | Restrict or application-managed cleanup |
| User → LoginHistories | Restrict (records are retained for audit) |
| User → AuditLogs | Restrict (records are retained for audit) |
| System → UserSystemAccess | Restrict (access records are retained) |
| System → AdministratorSystemAccess | Restrict (management grants are retained) |
| System → SystemSettings | Restrict (settings retained with parent system) |

---

## Key Constraints Summary

| Constraint | Table | Column(s) | Notes |
|---|---|---|---|
| UNIQUE | `Roles` | `Code` | Programmatic role code |
| UNIQUE | `Divisions` | `Code` | Division short code |
| UNIQUE | `Sections` | `(DivisionId, Code)` | Scoped within parent Division |
| UNIQUE | `Users` | `EmployeeNumber` | Official employee business identifier |
| UNIQUE | `Users` | `Username` | Unique login identifier |
| UNIQUE | `Systems` | `SystemCode` | Canonical system code |
| UNIQUE | `SystemSettings` | `(SystemId, SettingKey)` | Unique setting key per system |
| UNIQUE | `UserSystemAccess` | `(UserId, SystemId)` | Exactly one rule per user per system |
| UNIQUE | `AdministratorScopes` | `AdministratorUserId` | Exactly one division scope per Administrator |
| UNIQUE | `AdministratorSystemAccess` | `(AdministratorUserId, SystemId)` | Exactly one grant per admin per system |

---

## Relationship Diagram

See [../System-Architecture/ERD.md](../System-Architecture/ERD.md) for the full Mermaid ERD.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Tables | [Tables.md](Tables.md) |
| Schema | [Schema.md](Schema.md) |
| Indexes | [Indexes.md](Indexes.md) |
| ERD | [../System-Architecture/ERD.md](../System-Architecture/ERD.md) |
