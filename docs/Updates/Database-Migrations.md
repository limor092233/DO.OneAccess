# Updates — Database Migrations

> **Section:** Updates  
> **Document:** Database Migrations Process  
> **Related:** [Update-Process.md](Update-Process.md) | [../Database/Migration-Strategy.md](../Database/Migration-Strategy.md)

---

## Overview

All database schema changes in DO.OneAccess are managed via **EF Core Migrations**. This document describes the process for creating and applying migrations as part of the change management workflow.

---

## When to Create a Migration

A new migration is required whenever:
- A new table is added.
- A column is added, renamed, or removed.
- A data type is changed.
- A constraint (PK, FK, UNIQUE, INDEX) is added, changed, or removed.
- Seed data is added or changed.

---

## Migration Creation Process

1. **Make the model change** in the relevant EF Core entity configuration (`IEntityTypeConfiguration<T>`).
2. **Generate the migration:**
   ```bash
   dotnet ef migrations add {DescriptiveName} \
     --project src/DO.OneAccess.Infrastructure \
     --startup-project src/DO.OneAccess.Server
   ```
3. **Review the generated migration file** — Verify `Up()` and `Down()` are correct.
4. **Update documentation:**
   - Update [../Database/Tables.md](../Database/Tables.md) if columns changed.
   - Update [../Database/Relationships.md](../Database/Relationships.md) if FKs changed.
   - Update [../Database/Indexes.md](../Database/Indexes.md) if indexes changed.
   - Update [../System-Architecture/ERD.md](../System-Architecture/ERD.md) if entity relationships changed.
5. **Add a changelog entry** in [Changelog.md](Changelog.md).

---

## Migration Naming Convention

```
{Timestamp}_{PascalCaseDescription}

Examples:
20260911030348_InitialCreate
20260915000000_AddSystemCodeUniqueConstraint
```

---

## Migration History

| Migration | Date | Description |
|---|---|---|
| `20260911030348_InitialCreate` | 2026-09-10 | Baseline schema creation: initial tables, keys, constraints, indexes, and 3 seed roles |
| `20260911181658_MakeEmployeeSectionIdNullable` | 2026-09-11 | Made SectionId nullable on employee entity to allow universal scope |
| `20260912034320_ConsolidateEmployeeIntoUser` | 2026-09-12 | Consolidated Employee entity into User; removed Employees table; established 12 canonical tables with EmployeeNumber |

---

## Applying Migrations

### Development
```bash
dotnet ef database update \
  --project src/DO.OneAccess.Infrastructure \
  --startup-project src/DO.OneAccess.Server
```

### Staging / Production
- Generate a SQL script for the pending migration:
  ```bash
  dotnet ef migrations script --idempotent \
    --project src/DO.OneAccess.Infrastructure \
    --startup-project src/DO.OneAccess.Server \
    --output migration.sql
  ```
- Review the script.
- Apply via a controlled deployment step (not automatic on startup, for production).
- Verify migration success before routing traffic to the new application version.

---

## Rules

| Rule | Detail |
|---|---|
| Never edit an applied migration | Once a migration is applied to any environment, it must not be modified |
| Always include Down() | Provide a rollback path where feasible |
| Test on staging first | All migrations must be validated on staging before production |
| Version control | All migration files are committed to source control |
| No manual SQL scripts | Schema changes go through EF Core migrations only (with rare documented exceptions) |

---

## Rollback

- EF Core does not support automatic rollback to a previous migration in SQL Server with all operations.
- For rollback: apply the previous migration's `Down()` via `dotnet ef database update {PreviousMigrationName}`.
- For production rollbacks: use the SQL migration script approach and have a rollback plan before applying.

---

## Related Documents

| Document | Link |
|---|---|
| Migration Strategy | [../Database/Migration-Strategy.md](../Database/Migration-Strategy.md) |
| Update Process | [Update-Process.md](Update-Process.md) |
| Release Process | [Release-Process.md](Release-Process.md) |
