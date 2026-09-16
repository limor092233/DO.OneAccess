# Updates — Release Process

> **Section:** Updates  
> **Document:** Release Process  
> **Status:** Placeholder — to be detailed when CI/CD is established  
> **Related:** [Update-Process.md](Update-Process.md) | [Database-Migrations.md](Database-Migrations.md) | [Changelog.md](Changelog.md)

---

## Purpose

This document will define the end-to-end process for releasing a new version of DO.OneAccess from development through production.

---

## Placeholder Topics

The following will be documented when CI/CD infrastructure and release tooling are established:

- [ ] Versioning scheme (e.g., semantic versioning `MAJOR.MINOR.PATCH`).
- [ ] Branch and merge strategy (e.g., feature branches → main → release).
- [ ] CI pipeline steps (build, test, lint, publish).
- [ ] CD pipeline steps (deploy to staging, smoke test, deploy to production).
- [ ] Database migration application as part of release.
- [ ] Rollback procedure for failed releases.
- [ ] Release approval gates (e.g., staging sign-off before production).
- [ ] Changelog update requirement for each release.
- [ ] Communication plan for releases affecting users.

---

## Immediate Rules (Pre-CI/CD)

Until a formal CI/CD process is in place:

| Rule | Detail |
|---|---|
| Build before deployment | Run `dotnet build` and all tests before any deployment |
| Migrations before app | Apply database migrations before starting the updated application |
| Staging before production | All releases must be validated in staging before production |
| Document changes | Update CHANGELOG for each release |
| No secrets in artifacts | Published artifacts must not contain secrets |

---

## Release Checklist (Minimal)

- [ ] All tests pass.
- [ ] Documentation is up to date.
- [ ] Changelog entry added.
- [ ] Migration validated on staging.
- [ ] Staging smoke test passed.
- [ ] Deployment approved.

---

## Related Documents

| Document | Link |
|---|---|
| Update Process | [Update-Process.md](Update-Process.md) |
| Database Migrations | [Database-Migrations.md](Database-Migrations.md) |
| Changelog | [Changelog.md](Changelog.md) |
| Staging | [../Deployment/Staging.md](../Deployment/Staging.md) |
| Production | [../Deployment/Production.md](../Deployment/Production.md) |
