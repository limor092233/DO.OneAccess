# Database — Seed Data

> **Section:** Database  
> **Document:** Seed Data  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Tables.md](Tables.md) | [Migration-Strategy.md](Migration-Strategy.md) | [../Access-Control/Roles.md](../Access-Control/Roles.md)

---

## Purpose

Seed data provides the minimum reference data required for DO.OneAccess to function correctly at initialization. It is applied once during the initial database setup.

---

## Seed Data Categories

| Category | Table | When Applied |
|---|---|---|
| Role definitions | `Roles` | Initial migration or HasData seeding |
| Initial System Administrator | `Users` | Via the one-time initialization endpoint (`POST /api/setup`); NOT via migration |

---

## 1. Roles Seed Data

The `Roles` table must be seeded with exactly three records:

| Field | System Administrator | Administrator | User |
|---|---|---|---|
| `RoleId` (`smallint`) | `1` | `2` | `3` |
| `Code` | `SYSTEM_ADMINISTRATOR` | `ADMINISTRATOR` | `USER` |
| `Name` | `System Administrator` | `Administrator` | `User` |
| `Description` | Global platform administrator | Division-scoped administrator | Regular employee user |
| `IsActive` | `true` | `true` | `true` |

> **Semantic Distinction Rule:** `Code` and `Name` are distinct and must not be treated as interchangeable. `Code` is the immutable programmatic identifier (used in authorization checks and constant definitions), while `Name` is the human-readable display label.

---

## 2. Initial System Administrator

The first System Administrator is **not** seeded via a migration. It is created through the **one-time secure initialization endpoint** (`POST /api/setup`).

This approach:
- Avoids embedding credentials in migrations or seed scripts.
- Requires the deployment operator to complete initialization after first deployment.
- Prevents re-initialization after the System Administrator exists.

See [../Security/Authentication.md](../Security/Authentication.md) for the initialization flow.

---

## Seed Data Rules

| Rule | Detail |
|---|---|
| Roles are reference data | Role records must not be deleted |
| Fixed numeric RoleIds | Stable `smallint` identifiers (`1`, `2`, `3`) consistent across all environments |
| Distinct Code and Name | `Code` and `Name` must be seeded as defined; do not conflate them |
| No default passwords in migrations | Initial user creation is handled via the initialization endpoint |
| No secrets in seed data | Seed scripts must not contain passwords, secrets, or tokens |

---

## Applying Seed Data

Seed data is applied via EF Core's `HasData()` method in the `AppDbContext` `OnModelCreating()`:

```csharp
// Illustrative — not implementation
modelBuilder.Entity<Role>().HasData(
    new Role { RoleId = 1, Code = "SYSTEM_ADMINISTRATOR", Name = "System Administrator", Description = "Global platform administrator", IsActive = true },
    new Role { RoleId = 2, Code = "ADMINISTRATOR", Name = "Administrator", Description = "Division-scoped administrator", IsActive = true },
    new Role { RoleId = 3, Code = "USER", Name = "User", Description = "Regular employee user", IsActive = true }
);
```

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Roles | [../Access-Control/Roles.md](../Access-Control/Roles.md) |
| Migration Strategy | [Migration-Strategy.md](Migration-Strategy.md) |
| Initial Setup Authentication | [../Security/Authentication.md](../Security/Authentication.md) |
