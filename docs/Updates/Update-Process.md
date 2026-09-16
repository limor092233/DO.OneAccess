# Updates — Update Process

> **Section:** Updates  
> **Document:** Update Process  
> **Related:** [Database-Migrations.md](Database-Migrations.md) | [Release-Process.md](Release-Process.md)

---

## Purpose

This document describes the process for making changes to DO.OneAccess, including code changes, database schema changes, and documentation updates.

---

## Change Categories

| Category | Examples | Documentation Requirement |
|---|---|---|
| Architecture change | New layer, new project, dependency direction change | Update System-Architecture docs |
| Security change | Authentication algorithm, token lifetime, new auth flow | Update Security docs |
| Database schema change | New table, new column, constraint change | Update Database docs + ERD + migration |
| API change | New endpoint, changed request/response shape | Update Backend/API-Design.md |
| Access control change | New role, changed scope rules | Update Access-Control docs |
| Frontend change | New page, navigation change, auth flow change | Update Frontend docs |
| Integration change | New integration identifier, changed contract | Update Integration docs |
| Deployment change | New environment, new secret management approach | Update Deployment docs |

---

## Documentation Rule

> **Any significant change involving Architecture, Security, Authentication, Authorization, Database, Access Control, Frontend architecture, Backend architecture, Deployment, or Integration must update the corresponding documentation under `/docs` as part of the same change.**

Documentation updates are not optional add-ons. They are part of the definition of done for each change.

---

## Change Process

1. **Identify the change type** — What area is being changed?
2. **Review existing documentation** — Read the relevant `/docs` section before implementing.
3. **Implement the change** — Code, migrations, configuration.
4. **Update documentation** — Update all affected `/docs` files.
5. **Update CHANGELOG** — Add an entry in [Changelog.md](Changelog.md).
6. **Review** — Peer review should include documentation review.
7. **Deploy via release process** — See [Release-Process.md](Release-Process.md).

---

## Prohibited Change Patterns

| Pattern | Rule |
|---|---|
| Inventing business rules | Do not add business rules not present in the approved specification. Stop and report instead. |
| Silent architectural decisions | Do not change architecture without approval. Document and discuss first. |
| Secrets in code | Never commit secrets to source control. |
| Breaking integration identity | Do not change `UserId` or `SystemId` assignment logic without explicit approval. |
| Undocumented schema changes | All schema changes must be accompanied by documentation and a migration. |

---

## Related Documents

| Document | Link |
|---|---|
| Database Migrations | [Database-Migrations.md](Database-Migrations.md) |
| Release Process | [Release-Process.md](Release-Process.md) |
| Changelog | [Changelog.md](Changelog.md) |
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
