# Database — Migration Strategy

> **Section:** Database  
> **Document:** Migration Strategy  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Schema.md](Schema.md) | [../Updates/Database-Migrations.md](../Updates/Database-Migrations.md)

---

## Migration Tool

DO.OneAccess uses **EF Core Migrations** (`dotnet ef migrations`) to manage database schema changes.

---

## Migration Principles

| Principle | Rule |
|---|---|
| Code-first | Schema is defined in EF Core entity configurations; migrations are generated from model changes |
| Version-controlled | All migration files are committed to source control |
| Reversible | Migrations should include a `Down()` method where feasible |
| No manual SQL | Schema changes are applied via EF Core migrations, not manual scripts (except for edge cases documented separately) |
| Idempotent | Migrations must be safe to apply in sequence without manual intervention |
| Never modify generated files post-apply | Once a migration is applied to any environment, do not edit it |

---

## Migration Naming Convention

```
{Timestamp}_{PascalCaseDescription}

Examples:
20260901000000_InitialCreate
20260910000000_AddRefreshTokenRevokedAt
20260915000000_AddSystemCodeUniqueConstraint
```

---

## Initial Migration & Evolution

The initial migration (`20260911030348_InitialCreate`) created the initial baseline tables with:
- Primary keys (`<Entity>Id`).
- Foreign key constraints.
- Unique constraints.
- Indexes.

Subsequent migrations (`20260911181658_MakeEmployeeSectionIdNullable` and `20260912034320_ConsolidateEmployeeIntoUser`) evolved the schema to the current 12 canonical tables where employee profile data is consolidated directly into `Users`.

---

## Applying Migrations

**Development:**
```bash
dotnet ef database update --project DO.OneAccess.Infrastructure --startup-project DO.OneAccess.Server
```

**Production:**
- Migrations are applied as part of the release process.
- Application startup may optionally apply pending migrations automatically (`context.Database.MigrateAsync()`).
- For production, prefer explicit migration application over automatic startup migration.
- See [../Updates/Database-Migrations.md](../Updates/Database-Migrations.md) for the full process.

---

## Seed Data

Seed data (Roles, initial configuration) is applied via EF Core data seeding (`modelBuilder.Entity<T>().HasData(...)`) in the initial migration or a dedicated seeder.

See [Seed-Data.md](Seed-Data.md).

---

## Related Documents

| Document | Link |
|---|---|
| Seed Data | [Seed-Data.md](Seed-Data.md) |
| Database Updates | [../Updates/Database-Migrations.md](../Updates/Database-Migrations.md) |
| Schema | [Schema.md](Schema.md) |
