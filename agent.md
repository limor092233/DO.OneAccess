# DO.OneAccess — AI Agent Engineering Guide

> **Document:** Official AI Agent Engineering Guide  
> **Target Path:** `.\agent.md`  
> **Classification:** Internal Project Standard & Operational Protocol  
> **Status:** Authoritative / Active  

---

## 1. Core Operating Principles & Execution Posture

All AI agents assisting with the **DO.OneAccess** repository must operate strictly in accordance with this engineering guide. The latest approved Project Lead instructions and update records take absolute precedence over older plans, external defaults, or outdated documentation.

### 1.1 Read-Only Default & Approval Gates
- **Default Mode:** Read-only inspection and analysis.
- **Pre-Execution Gate:** Before modifying any project files, the agent must define and present:
  - **Scope:** Exact scope and objectives of the change.
  - **Files to Modify:** Explicit list of target files.
  - **Files NOT to Modify:** Explicit list of files preserved and left untouched.
  - **Risks & Impact:** Assessment of architectural, security, database, or UI implications.
  - **Validation Plan:** Exact build, test, and verification commands.
  - **Rollback Plan:** Clear recovery steps if changes need to be reverted.
- **Explicit Approval:** Major architectural changes, schema migrations, credential updates, or breaking API changes require explicit approval before applying.

### 1.2 Discrepancy & Conflict Resolution Protocol
When code, tests, migrations, database schema, and documentation disagree:
1. **Never Guess:** Do not assume which source is correct.
2. **Report Exact Conflict:** Document the discrepancy precisely across all conflicting artifacts.
3. **Identify Affected Files:** List every file impacted by the conflict.
4. **Explain Impact:** Clarify behavioral, data, or security risks.
5. **Propose Safe Resolution:** Outline a concrete, non-destructive resolution path.
6. **Seek Approval:** Request approval from the Project Lead or user when needed before proceeding.

---

## 2. Configuration & Secrets Management

Strict configuration rules are enforced to preserve environment isolation and operational security.

### 2.1 Appsettings-Only Standard
- **Mandatory Storage:** ALL application configuration must be stored in `appsettings` files (`appsettings.json`, `appsettings.Development.json`, etc.).
- **Development Configuration:** For local development, configuration must reside in:
  `src/DO.OneAccess.Server/appsettings.Development.json`
- **Development JWT Key:** The development JWT signing key (`Jwt:Key`) must remain in:
  `src/DO.OneAccess.Server/appsettings.Development.json`

### 2.2 Prohibited Configuration Patterns
- **NO .NET User Secrets:** .NET User Secrets (`Microsoft.Extensions.Configuration.UserSecrets`) must **NOT** be used.
- **Do NOT:**
  - Add `UserSecretsId` to `.csproj` files.
  - Call `.AddUserSecrets()` in configuration builders or host setup.
  - Move `Jwt:Key` to User Secrets.
  - Move application configuration to arbitrary environment variables without explicit design approval.
  - Introduce hidden hard-coded configuration fallback values (e.g., hardcoded fallback keys, arbitrary CORS defaults, or fallback rate limits that mask missing configuration). Fail-fast startup configuration binding is required.
  - Introduce alternative configuration providers without explicit approval.

---

## 3. Database Safety & Inspection Rules

### 3.1 Strict Safety Prohibitions
- **Never** drop or reset the database.
- **Never** delete production or existing persistent data.
- **Never** rewrite, edit, or delete an applied migration.
- **Never** modify database records manually via ad-hoc SQL or scripts during audits.
- **Never** execute destructive SQL (`DROP`, `TRUNCATE`, uncontrolled `DELETE`).
- **Never** run `POST`, `PUT`, `PATCH`, or `DELETE` requests during read-only audits.
- **Never** remove existing working features merely to simplify code.

### 3.2 Comprehensive Database Model Reconciliation
**Never determine the database table count or schema shape using only `DbSet<T>` properties.**

When verifying or inspecting the database model, agents must reconcile all thirteen (13) sources of truth:
1. `DbSet<T>` properties on `OneAccessDbContext`
2. EF Core entity type registrations
3. `OnModelCreating` configuration methods
4. `IEntityTypeConfiguration<T>` mapping classes
5. All migration history files (`Migrations/*.cs`)
6. EF Core model snapshot (`*ModelSnapshot.cs`)
7. `CreateTable` operations in migrations
8. `RenameTable` / `RenameColumn` operations in migrations
9. Explicit join tables and payload-free many-to-many configurations
10. Database seed operations and initial data scripts
11. Actual SQL Server physical schema (`INFORMATION_SCHEMA`, sys tables)
12. Database documentation (`docs/Database/*.md`)
13. Entity Relationship Diagram documentation (`docs/System-Architecture/ERD.md`)

### 3.3 Migration Protocol
- Applied migrations are immutable.
- Every new migration requires:
  - Explicit approval before creation.
  - Detailed review of both `Up()` and `Down()` methods.
  - Full impact analysis on schema, entities, and data.
  - Synchronization of all database documentation and ERD.
  - Update entry in `docs/Updates/Changelog.md`.
  - Clean build and passing automated test execution.

---

## 4. First-Run Setup & Bootstrap Rules

When performing or verifying the first-run system administrator bootstrap workflow:

### 4.1 Pre-Setup Verification
Before executing or testing first-run setup, confirm:
1. SQL Server target instance and connection string.
2. Target database name.
3. EF Core migration status (all migrations applied).
4. `Users` table count.
5. `Roles` table count.
6. `AuditLogs` table count.
7. Existing System Administrator account count.
8. Database state is confirmed clean and pre-bootstrap.

### 4.2 During Setup Execution
1. Use the official application UI workflow (`/setup`).
2. Do not manually insert database records or bypass the application layer.
3. Do not repeatedly call the setup endpoint.
4. Do not expose bootstrap tokens, credentials, or passwords in logs, terminal outputs, or artifacts.
5. Do not modify the database schema during setup.
6. Do not bypass backend or client-side validation rules.

### 4.3 Post-Setup Verification
1. Verify the newly created account through the Blazor WebAssembly UI.
2. Verify interactive login.
3. Verify interactive logout.
4. Verify protected-route and role-authorization behavior.
5. Verify expected user count.
6. Verify System Administrator role assignment.
7. Verify generated audit log entry for the setup action.
8. Update corresponding documentation under `/docs`.
9. Add an entry to `docs/Updates/Changelog.md`.

---

## 5. Documentation & Change Management

Documentation integrity is an essential requirement. Code changes without corresponding documentation updates are considered incomplete.

### 5.1 Mandatory Documentation Synchronization
- **Rule:** Any system change involving Architecture, Security, Authentication, Authorization, Database, Access Control, Frontend architecture, Backend architecture, Deployment, or Integration **must** update the corresponding documentation files under `docs/` in the same task.
- **Changelog Requirement:** Every system change must update `docs/Updates/Changelog.md`.

### 5.2 Changelog Entry Standard
Every entry added to `docs/Updates/Changelog.md` must clearly include:
- **Date:** ISO format (e.g., `YYYY-MM-DD`).
- **Change Title:** Descriptive summary of the change.
- **Reason:** Business or technical rationale.
- **Exact Files Changed:** Absolute or relative paths of modified files.
- **Database Impact:** Schema, migration, seed, or index impact.
- **API Impact:** Endpoint, routing, or DTO contract changes.
- **UI Impact:** Component, layout, navigation, or state changes.
- **Security Impact:** Authentication, authorization, token, or audit log impact.
- **Tests Executed and Results:** Exact command output summary, pass/fail counts.
- **Documentation Updated:** List of all markdown files revised.
- **Rollback Notes:** Instructions to revert the change safely.

---

## 6. Build, Testing & Validation Standards

### 6.1 Standard Verification Commands
For backend, domain, application, infrastructure, and full-system verification:
```powershell
dotnet build DO.OneAccess.sln
dotnet test DO.OneAccess.sln --no-build
```

### 6.2 Truth in Verification Reporting
Agents must never claim tests passed without verified execution output. Always report:
- Build warnings and build errors (if any).
- Unit test execution count and status.
- Integration test execution count and status.
- Failed tests (with failure stack trace/reason).
- Skipped tests (with reason).
- Manual UI test results (where browser testing is performed).

---

## 7. Architectural Context & System Guidelines

| Attribute | Specification |
|---|---|
| **Architecture Pattern** | Onion Architecture (`Domain` → `Application` → `Infrastructure` / `Server` / `Client`) |
| **Runtime & Language** | .NET 10 / C# |
| **API Framework** | ASP.NET Core Web API |
| **Frontend Framework** | Blazor WebAssembly |
| **Database & ORM** | SQL Server / Entity Framework Core |
| **Authentication** | JWT (short-lived access tokens, hashed refresh tokens in HttpOnly Secure cookies) |
| **Authorization** | Role-based with scoped administrative domain boundaries (System Administrator, Administrator, User) |
| **Error Handling** | RFC 7807 `ProblemDetails` / `ValidationProblemDetails` |
| **Confidentiality** | Never log, print, or commit plaintext passwords, JWT secret keys, or bootstrap tokens |

---

*This document serves as the permanent, authoritative guidance for all AI agent interactions within the DO.OneAccess repository.*
