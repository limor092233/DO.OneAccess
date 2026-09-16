# Changelog

> **Section:** Updates  
> **Document:** Changelog  
> **Format:** Newest first

## Documentation Reconciliation — 2026-09-16

### Changed
- **Entity, Identifier & Schema Reconciliation:**
  - Preserved the HR-determined business identifier requirement while documenting the exact technical property name `EmployeeNumber` (`nvarchar(50)` on `Users` entity).
  - Explicitly distinguished internal technical identity `UserId` (`Guid` / `uniqueidentifier`) from the HR-managed business identifier `EmployeeNumber`.
  - Reconciled all database documentation (`Tables.md`, `Schema.md`, `Relationships.md`, `Indexes.md`, `Database-Architecture.md`, `ERD.md`) to reflect the 12 canonical tables following `ConsolidateEmployeeIntoUser` migration.
  - Updated `Database-Migrations.md` to record all applied EF Core migrations (`20260911030348_InitialCreate`, `20260911181658_MakeEmployeeSectionIdNullable`, `20260912034320_ConsolidateEmployeeIntoUser`).
- **API & Routing Alignment:**
  - Fixed `POST /api/setup` endpoint documentation across `Seed-Data.md`, `API-Design.md`, and `DO.OneAccess-Specification-v1.md` (removing obsolete `/api/auth/setup` references).
  - Reconciled `/api/system-access` documentation in `API-Design.md` to match actual controller endpoints (`POST` for setting access overrides and `DELETE` for revocation).
  - Reconciled DTO examples in `API-Design.md` to use `userId` rather than obsolete `employeeId`.
- **Validation & Library Claims Harmonization:**
  - Updated `Validation.md`, `Architecture.md`, `Project-Structure.md`, and `Component-Guidelines.md` to reflect the actual custom Application-layer validator classes (`FirstRunSetupDtoValidator`) and service-level validations mapping to RFC 7807 `ProblemDetails` / `ValidationProblemDetails`, removing stale claims of external `FluentValidation` package usage.
- **Security & Configuration Alignment:**
  - Corrected JWT Audience example in `Security-Guidelines.md` to `"DO.OneAccess.API"`, matching `appsettings.json` and `JwtTokenService.cs`.
  - Reconciled client key files in `Frontend/Project-Structure.md` to reflect `CustomAuthorizationMessageHandler.cs`, `ClientAuthService.cs`, and `ProblemDetailsReader.cs`.

## [1.14.0] — 2026-09-15 — Strict Appsettings-Only Configuration & System Compliance

### Changed
- **Application Configuration Storage Harmonization:**
  - Enforced Authoritative Project Lead Requirement: All application configuration must be stored in `appsettings` files; .NET User Secrets must NOT be used for application configuration.
  - Development `Jwt:Key` is maintained in `src/DO.OneAccess.Server/appsettings.Development.json`.
  - Added `Cors:AllowedOrigins` to `appsettings.json` and `appsettings.Development.json`.
  - Added `Security:SetupRateLimitPermitLimit` (10) and `Security:SetupRateLimitWindowSeconds` (60) to `appsettings.json` and `appsettings.Development.json`.
- **Program.cs Fail-Fast Hardening:**
  - Removed hardcoded fallback string for `Jwt:Key`; enforced fail-fast exception rejecting null, empty, or whitespace-only keys, as well as keys under 32 bytes (256 bits).
  - Removed hardcoded CORS origins array; bound dynamically to `Cors:AllowedOrigins` with validation requiring at least one non-empty origin.
  - Removed `?? 10` and `?? 60` rate-limiting fallbacks; bound dynamically to configuration with positive-integer validation.
- **Documentation Alignment:**
  - Updated all `/docs` references to remove `.NET User Secrets` guidance and align with the `appsettings` files model.

## [1.13.0] — 2026-09-12 — UI/UX Refinements: User Creation, Division Creation & Hierarchical Division Management

### Changed
- **Create New User Account Modal (`ManageUsers.razor`):**
  - Reorganized into structured sections: Employee Information, Organizational Assignment, and Account.
  - Implemented conditional Section field behavior: displayed only when selected Division contains active Sections, and hidden completely if no active Sections exist.
  - Reset Section selection and re-evaluated availability whenever Division selection changes.
  - Enforced client-side validation preventing regular User creation in divisions lacking active sections.
  - Maintained role rules: User accounts require Section; Administrator accounts retain optional Section with authority deriving from Division scope.
  - Added field-level inline validation, password visibility toggle, responsive two-column grid, and non-duplicate submission states ("Creating User...").
- **Create New Division Modal (`ManageDivisions.razor`):**
  - Added unique short identifier helper text for Division Code.
  - Added inline field-level validation and submission busy indicator ("Creating Division...").
- **Manage Division Page & Hierarchical Table (`ManageDivisions.razor`):**
  - Replaced nested tables with a clean hierarchical expandable/collapsible table displaying parent Divisions and indented child Sections (`Division └── Section`) with SVG hierarchy icons.
  - Implemented table-owned vertical scrolling container with sticky header anchored to the table container, eliminating window-level vertical scrolling.
  - Added multi-target search filtering across Division Name, Division Code, Section Name, and Section Code with automatic expansion and revelation of matching parent divisions.
  - Implemented compact 3-dot dropdown action menus for Divisions and Sections with single-menu activation, stop-propagation to prevent accidental row toggles, and click-outside dismissal.
- **Common Icon Library (`Icon.razor`):**
  - Added `chevron-down`, `chevron-right`, `more-vertical`, and `corner-down-right` SVG icons.

## [1.12.0] — 2026-09-12 — Business Rule Corrections: Division-Section Hierarchy, Exactly-One SysAdmin Invariant & Role Transfer

### Changed
- **Organizational Hierarchy Alignment (Division → Section → User):**
  - Clarified and preserved `User.SectionId` as the sole organizational relationship for standard USER accounts without adding `Users.DivisionId` column.
  - Organizational hierarchy flows strictly as `Division owns/manages child Sections; Section owns/assigns Users`.
  - Updated Create User workflow to feature dependent `Division → Section` selection:
    - Normal `USER`: Section selection is required and must belong to selected Division.
    - `ADMINISTRATOR`: Section is fixed to `NULL`; Division selection establishes `AdministratorScopes.DivisionId`.
  - Updated `IDivisionService.GetAllDivisionsAsync` and `GET /api/divisions`:
    - Accessible under `AdministratorOrAbove` policy.
    - System Administrator receives all organizational divisions.
    - Administrator receives only their scoped Division.
  - Option A Routing:
    - System Administrator uses `/sysadmin/divisions` with hierarchical view and management of Divisions and connected child Sections.
    - Administrator uses `/admin/divisions` displaying their assigned Division profile and child Sections table.
    - Sidebar navigation (`NavMenu.razor`) routes Administrator directly to `/admin/divisions`.

- **Exactly-One System Administrator Invariant & Atomic Role Transfer:**
  - Removed old privileged System Administrator creation endpoint (`POST /api/users/system-administrator`) and UI.
  - The application guarantees the exactly-one System Administrator invariant for supported application workflows by restricting SysAdmin assignment to First Run Setup and the atomic Role Transfer workflow, protected by a Serializable transaction and post-condition validation.
  - Implemented atomic `POST /api/users/system-administrator/transfer`:
    - Current active System Administrator transfers role to an active User or Administrator.
    - Target account receives `RoleId = SYSTEM_ADMINISTRATOR` and `SectionId = NULL`.
    - Intentional lifecycle cleanup: target's prior `AdministratorScopes` and `AdministratorSystemAccess` records are removed.
    - Departing System Administrator transitions to a valid `ADMINISTRATOR` (requires Division assignment) or `USER` (requires Section assignment within chosen Division).
    - All active refresh tokens for both accounts are revoked, enforcing fresh credential verification.
    - Protected by `IsolationLevel.Serializable` transaction and post-condition invariant check `COUNT(Users WHERE RoleId = 1) == 1`.
    - Historical audit logs and login histories are fully preserved; emits audit action `TRANSFER_SYSTEM_ADMINISTRATOR`.

## [1.11.0] — 2026-09-12 — Employee/User Entity Consolidation & Privileged SysAdmin Architecture

### Changed
- Architectural Consolidation (**ONE EMPLOYEE = ONE DO.OneAccess USER**):
  - Consolidated the `Employee` entity and database table directly into `User`.
  - Canonical database table count reduced from 13 to 12 (`Employees` table removed).
  - `UserId` established as the single stable technical identity across the platform.
  - `EmployeeNumber` remains the official unique business identifier on `Users`.
  - `Users.SectionId` is nullable at database level (`int NULL`):
    - `SYSTEM_ADMINISTRATOR`: `SectionId = NULL` (global scope).
    - `ADMINISTRATOR`: `SectionId` may be `NULL` (scope derives strictly from `AdministratorScopes.DivisionId`).
    - `USER`: `SectionId` is required (resolves through `User -> Section -> Division`).
  - `Users.Position` defined as nullable (`nvarchar(100) NULL`), matching approved First Run Setup behavior without synthetic fallbacks.
  - Retired separate `/api/employees` and merged profile management into `/api/users`.
  - Added compatibility route alias `@page "/admin/employees"` on `ManageUsers.razor`.

### Added
- Dedicated Privileged System Administrator Creation:
  - Created `POST /api/users/system-administrator` for active System Administrators only.
  - Backend strictly fixes `RoleId = SYSTEM_ADMINISTRATOR`, `SectionId = NULL`, and creates no `AdministratorScopes` record.
  - Excluded System Administrator from normal `POST /api/users` creation options.
  - Added dedicated audit log action `CREATE_SYSTEM_ADMINISTRATOR`.

## [1.10.0] — 2026-09-12 — Navigation Restructuring (Phase 1 & Phase 2)

### Added
- Registered Systems Portal Landing (`/systems`):
  - Added primary authenticated landing route `/systems` served by `Dashboard.razor`.
  - Updated PageTitle and UI context to "Registered Systems".
  - Retained `/dashboard` as an authenticated compatibility route.
  - Updated authenticated landing and recovery redirects from `/dashboard` to `/systems` in `Home.razor`, `Login.razor`, `TopBar.razor`, `NotFound.razor`, and `AccessDenied.razor`.
  - Updated error page copy to "Return to Registered Systems" targeting `/systems`.

### Changed
- Navigation Hierarchy & Sidebar (`NavMenu.razor`):
  - Restructured sidebar into approved 3-tier hierarchy:
    - **PORTAL**: Registered Systems (`/systems`) for all authenticated users (`USER`, `ADMINISTRATOR`, `SYSTEM_ADMINISTRATOR`).
    - **ADMINISTRATION**: Manage Users (`/admin/users`), Manage Division (`/sysadmin/divisions` for System Administrator; safe fallback `/admin/sections` for Administrator), and System Access (`/admin/system-access`).
    - **SYSTEM ADMINISTRATION**: Admin Scopes (`/sysadmin/scopes`), Audit Logs (`/sysadmin/audit-logs`), and Login History (`/sysadmin/login-histories`) strictly for `SYSTEM_ADMINISTRATOR`.
  - Removed Dashboard from sidebar navigation across all roles.
  - Preserved all existing underlying routes and backend authorization policies intact.
- Route & Authorization Test Suite:
  - Updated `RouteAndAuthorizationGuardTests.cs` to verify that both `/systems` and `/dashboard` are authenticated routes on `Dashboard.razor`.
  - Preserved all role-restricted admin route assertions.

## [1.9.0] — 2026-09-11 — First Run System Administrator Setup & Secure System Bootstrap (Step 8)

### Added

- Database Schema & Entity Domain Alignment:
  - Made `Employees.SectionId` nullable (`int?`) in `Employee.cs` and `EmployeeConfiguration.cs` with `OnDelete(DeleteBehavior.Restrict)`, permitting System Administrators to have universal organizational scope without Division/Section assignment.
  - Generated and applied EF Core migration `20260911181658_MakeEmployeeSectionIdNullable` cleanly to development and integration databases.
  - Updated `EmployeeDto.cs`, `EmployeeService.cs`, and `ManageEmployees.razor` to support nullable sections and divisions safely with null-conditional operators.
- Core Application & Infrastructure Bootstrap Services:
  - Created `IBootstrapTokenService` and `BootstrapTokenService`:
    - Development: Ephemeral 256-bit cryptographic hex token generated on startup when uninitialized and printed to stdout with `[FIRST RUN SETUP]`.
    - Production: Reads `ONEACCESS_BOOTSTRAP_TOKEN` from environment/secrets; fails closed (503 Service Unavailable, warning log, no auto-generation) if missing.
    - Timing protection: Token verification uses `CryptographicOperations.FixedTimeEquals` for constant-time comparison.
    - Single-use invalidation: Token is wiped from memory immediately after successful provisioning.
  - Created `IFirstRunSetupService` and `FirstRunSetupService`:
    - Dynamic state derivation via `Users.AnyAsync(u => u.RoleId == 1)` (no dedicated initialization table).
    - Inactive SysAdmin lockout: Existence of any SysAdmin user permanently locks setup.
    - Transaction isolation: Executes all setup persistence (Employee, User, AuditLog) in a single atomic transaction with `IsolationLevel.Serializable`.
    - In-process concurrency protection via `SemaphoreSlim(1, 1)`.
    - Hardcoded server role assignment: `RoleId = (short)RoleType.SystemAdministrator` (client cannot supply or modify role).
    - Audit logging: Records system provisioning with `UserId = null`, `Action = "SYSTEM_BOOTSTRAP"`, `EntityName = "User"`, and `NewValues` serialized via `JsonSerializer.Serialize` strictly omitting passwords, hashes, and tokens.
  - Created `SetupController` (`/api/setup`):
    - `GET /api/setup/status`: `[AllowAnonymous]` with `[ResponseCache(NoStore = true)]`.
    - `POST /api/setup`: `[AllowAnonymous]`, header-only `X-Bootstrap-Token` transport, and endpoint rate limiting via `[EnableRateLimiting("SetupEndpointPolicy")]` returning 429 Too Many Requests upon limit exhaustion.
- Blazor WebAssembly Client First Run Setup Wizard:
  - Registered dedicated unauthenticated `HttpClient` for `ISetupApiClient` and `ISetupStateService` in `ServiceCollectionExtensions.cs`.
  - Built `Pages/FirstRunSetup.razor`: 3-step card wizard (Bootstrap Authorization, Employee Identity, System Administrator Credentials) with real-time field validation, submitting spinner, and success confirmation.
  - Updated `App.razor` startup state machine: Queries `/api/setup/status` once on load; displays loading spinner; displays inline connection error screen with retry button on network outage (preventing redirect loops); navigates uninitialized visits to `/setup`; redirects initialized visits away from `/setup` to `/login`.
  - Updated `Login.razor` to handle `?initialized=true` and `?alreadyInitialized=true` banner notifications.
- Comprehensive Test Suite:
  - Added 6 unit tests in `BootstrapTokenServiceTests.cs` verifying development token generation, production fail-closed behavior, constant-time validation, and invalidation.
  - Added 17 unit test cases in `FirstRunSetupDtoValidatorTests.cs` verifying required fields, email formatting, password confirmation matching, and missing password rejection.
  - Added 8 integration tests in `SetupControllerIntegrationTests.cs` verifying uninitialized status, 401 on missing/wrong token header, 201 creation with Serializable transaction and `UserId = null` audit log, 409 replay rejection, immediate login capability, parallel concurrency handling, inactive SysAdmin lockout, and 429 rate limiting.

### Changed

- Updated `DO.OneAccess-Specification-v1.md` (v1.3), `docs/Access-Control/Roles.md`, and `docs/Deployment/Development.md` removing deferred bootstrap placeholders and locking the completed first-run setup architecture.
- Updated `docs/Backend/API-Design.md`, `docs/Frontend/API-Integration.md`, `docs/Frontend/Navigation.md`, `docs/Frontend/UI-Design.md`, and `docs/Frontend/Project-Structure.md` with `/api/setup` contracts and client components.

---

## [1.8.1] — 2026-09-11 — Blazor WebAssembly Runtime Startup & CORS Fix

### Fixed

- Resolved client dependency injection circular dependency (`IClientAuthService` ↔ `HttpClient` ↔ `CustomAuthorizationMessageHandler`):
  - Created `DO.OneAccess.Client.Extensions.ServiceCollectionExtensions.AddClientServices` providing a dedicated, unauthenticated `HttpClient` for `ClientAuthService` (`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`).
  - Retained `CustomAuthorizationMessageHandler` exclusively on the authorized `HttpClient` injected into application REST API clients.
  - Eliminated UI thread call-stack exhaustion on startup, allowing `<App>` to mount cleanly replacing the static loading indicator.
- Fixed Client API base address targeting:
  - Added `src/DO.OneAccess.Client/wwwroot/appsettings.json` and `appsettings.Development.json` configuring `"ApiBaseUrl": "http://localhost:5111"`.
  - Configured `Program.cs` to resolve `ApiBaseUrl` with fallback to `http://localhost:5111/`, redirecting API traffic away from the client static dev server (port 5202).
- Configured Cross-Origin Resource Sharing (CORS) on `DO.OneAccess.Server`:
  - Registered `BlazorClientPolicy` in `Program.cs` with `.WithOrigins("http://localhost:5202", "https://localhost:7030").AllowAnyHeader().AllowAnyMethod().AllowCredentials()`.
  - Added `app.UseCors("BlazorClientPolicy")` preceding authentication and authorization middleware.

### Added

- Regression test suite:
  - Unit tests in `ClientDependencyInjectionTests.cs` verifying DI resolution without circular dependency, `AuthClient` handler separation, authorized client delegation, and `ApiBaseUrl` assignment.
  - Integration tests in `CorsIntegrationTests.cs` verifying allowed origins (`http://localhost:5202`, `https://localhost:7030`), rejection of untrusted origins, and credential transmission headers.

---

## [1.8.0] — 2026-09-11 — Foundation Release Readiness & Documentation Handoff (Step 7)

### Added

- Comprehensive Cross-Document Verification & Synchronization:
  - Updated Master Specification `docs/DO.OneAccess-Specification-v1.md` to Version 1.2 (*Foundation Verification Complete / Release Ready for Approved Scope*).
  - Synchronized and locked architectural decisions 4 and 5 covering role management governance and browser E2E status.
  - Aligned `docs/Access-Control/Roles.md` with approved role management rules (restricting UI role changes to `USER` and `ADMINISTRATOR`, prohibiting self-role modification, blocking UI assignment of `SYSTEM_ADMINISTRATOR`, and documenting deferred initial bootstrap).
  - Aligned `docs/Deployment/Development.md` to accurately document deferred initial System Administrator provisioning without setup UI.
- Release Readiness Validation:
  - Verified 11 of 11 canonical database uniqueness rules with real SQL Server integration tests.
  - Verified operational REST endpoints across success paths, 401 unauthenticated, 403 forbidden/scoping, and 400 validation error handling.
  - Confirmed 0 database schema changes, 0 new migrations, and 0 unintended code changes.
  - Documented known boundaries and prerequisites: browser E2E pending, initial bootstrap deferred, SSO/OIDC/OAuth not implemented.

---

## [1.7.0] — 2026-09-11 — Testing, Hardening, Security Validation & Quality Assurance (Step 6)

### Added

- Authentication Hardening Test Suite (`AuthHardeningIntegrationTests.cs`):
  - Expired JWT rejection (401 Unauthorized with standard challenge header).
  - Malformed JWT string rejection (401 Unauthorized).
  - Invalid signature rejection (401 Unauthorized).
  - Inactive user refresh guard (401 Unauthorized and automatic token revocation).
  - Refresh token rotation (verifying `RevokedAt` and `ReplacedByTokenId` assignment upon refresh).
  - Refresh token replay cascading revocation & audit logging.
  - CSRF protection on cookie-based refresh and logout endpoints (400 Bad Request on missing or invalid header).
  - Valid logout revoking refresh token, clearing HttpOnly cookie, and returning 204 No Content.
  - Idempotent logout without cookies returning 204 No Content.
- Authorization & Scope Penetration Test Suite (`ScopePenetrationIntegrationTests.cs`):
  - Cross-division isolation: Administrators attempting GET, POST, PUT, DELETE on sections or employees outside assigned division scope receive 403 Forbidden.
  - Privilege escalation prevention: Administrators attempting to create accounts with `SYSTEM_ADMINISTRATOR` or `ADMINISTRATOR` roles receive 403 Forbidden.
  - Self-deactivation prevention: Users, Administrators, and System Administrators attempting self-deactivation via `DELETE /api/users/{id}` receive 403 Forbidden.
  - Self-role change prevention: System Administrators attempting self-role modification via `PUT /api/users/{id}/role` receive 403 Forbidden.
  - System access override scope isolation: Administrators attempting to assign or revoke `UserSystemAccess` for systems outside their `AdministratorSystemAccess` grant receive 403 Forbidden.
  - Mid-session inactive actor rejection: Valid JWT presented by a deactivated user account is rejected with 403 Forbidden on state-mutating requests.
- Operational API Integration Test Suite (`ApiEndpointsIntegrationTests.cs`):
  - Employees (`/api/employees`): Unauthenticated 401, User 403, Admin listing/get 200, Admin create 201, Admin update 200, Admin deactivate 204.
  - Sections (`/api/sections`): Admin list/get 200, Admin create 201, Admin update 200, Admin deactivate 204.
  - Users (`/api/users`): Admin list/get 200, Admin create linked to employee 201, Admin update 200, Admin deactivate 204.
  - Divisions (`/api/divisions`): Admin list 403, SysAdmin list 200, SysAdmin create 201, SysAdmin get 200, SysAdmin update 200, SysAdmin deactivate 204.
  - Systems (`/api/systems`): User register 403, User accessible systems resolution 200, SysAdmin list all 200, SysAdmin register 201, SysAdmin get 200, SysAdmin update 200, SysAdmin deactivate 204.
  - Admin Scopes (`/api/admin-scopes`): SysAdmin assign division scope 200, get scopes 200, delete scope 204.
  - Admin System Access (`/api/admin-system-access`): SysAdmin grant system management authority 200, list 200, revoke 204.
  - System Access Overrides (`/api/system-access`): Admin create allow override 200, list overrides 200, revoke override 204.
- Database & Persistence Integrity Test Suite (`DatabaseConstraintTests.cs`):
  - Verification that EF Core / SQL Server throws `DbUpdateException` upon duplicate username, duplicate employee number, duplicate `(DivisionId, Code)` section, duplicate `(UserId, SystemId)` override, and duplicate `(AdministratorUserId, SystemId)` authority grant.
  - Verification of exactly 0 database migrations or schema modifications created.
- Client Component & Error Handler Unit Tests:
  - Added non-401 error status code pass-through tests (400, 403, 404, 500) to `CustomAuthorizationMessageHandlerTests.cs` confirming immediate response return without refresh or logout side-effects.
  - Created `ClientComponentBehaviorTests.cs` testing parameter bindings, page calculations, confirmation callbacks, and badge CSS/text rendering for `StatusBadge`, `Pagination`, `ConfirmDialog`, `LoadingSpinner`, and `EmptyState`.

### Changed

- Updated `docs/Backend/API-Design.md` to document that `POST /api/auth/logout` is `[AllowAnonymous]` with CSRF protection and HttpOnly cookie revocation, resolving the documentation discrepancy with `Authentication.md`.
- Documented `PUT /api/users/{userId}/role` under `SystemAdministratorOnly` policy in `docs/Backend/API-Design.md`.

---

## [1.6.0] — 2026-09-11 — Blazor WebAssembly Portal Implementation (Step 5)

### Added

- Foundation, Authentication & Security in `DO.OneAccess.Client`:
  - In-memory access token storage via `ClientAuthService`; zero tokens stored in `localStorage` or `sessionStorage`.
  - Refresh token persistence strictly through existing server HttpOnly cookie and CSRF header verification.
  - `CustomAuthorizationMessageHandler` with `SemaphoreSlim` 401 refresh deduplication:
    - Excludes `/api/auth/*` from token attachment and refresh recursion.
    - Concurrent 401 requests share and await a single refresh operation.
    - Original requests are retried at most once, preventing infinite retry loops.
    - Refresh failure immediately triggers logout, clears auth state, and redirects to `/login`.
  - `JwtAuthenticationStateProvider` extracting claims (`sub` -> `NameIdentifier`, `name` -> `Name`, `role` -> `Role`), supporting scalar and array roles with graceful anonymous fallback for missing or malformed tokens.
  - RFC 7807 `ProblemDetailsReader` and `ProblemDetailsException` extracting title, detail, status, and validation error dictionaries.
  - Lightweight, dependency-free SVG icon system (`Icon.razor`) with 21 icons and graceful unknown icon fallback.
- Typed REST API Clients in `DO.OneAccess.Client.Services.Api`:
  - `ISystemApiClient` / `SystemApiClient` (`/api/systems`, `/api/systems/all`, registration, updates, deactivation).
  - `IUserApiClient` / `UserApiClient` (`/api/users` query, creation, update, deactivation, role change).
  - `IEmployeeApiClient` / `EmployeeApiClient` (`/api/employees` query, creation, update, deactivation).
  - `IDivisionApiClient` / `DivisionApiClient` (`/api/divisions` query, creation, update, deactivation).
  - `ISectionApiClient` / `SectionApiClient` (`/api/sections` query, creation, update, deactivation).
  - `ISystemAccessApiClient` / `SystemAccessApiClient` (`/api/system-access` overrides query, allow/deny setting, revocation).
  - `IAdminScopeApiClient` / `AdminScopeApiClient` (`/api/admin-scopes`, `/api/admin-system-access`).
  - `IAuditApiClient` / `AuditApiClient` (`/api/audit-logs`, `/api/login-histories`).
- Reusable UI Components in `DO.OneAccess.Client.Components.Common`:
  - `LoadingSpinner`, `ErrorMessage` (with validation errors list), `EmptyState`, `ConfirmDialog`, `Pagination`, `ModalDialog`, `StatusBadge`.
- Navigation, Layout & Route Guards:
  - `App.razor` wrapping with `<CascadingAuthenticationState>` and `<AuthorizeRouteView>`.
  - `MainLayout.razor` with `TopBar.razor` header and conditional `NavMenu.razor` sidebar.
  - Role-driven navigation menu dynamically filtering tabs based on `User.IsInRole()`.
  - `RedirectToLogin.razor`, `AccessDenied.razor` (403), `NotFound.razor` (404), `Home.razor` (redirects to `/dashboard`).
- User Dashboard & System Launch Security:
  - `Dashboard.razor` dynamically loading accessible systems via `GET /api/systems`.
  - Secure external system launch using backend-configured trusted `BaseUrl` (`target="_blank" rel="noopener noreferrer"`).
  - Strict security guarantee: JWT tokens are never appended to URLs, query strings, or hash fragments.
- Administrative & System Administration Pages:
  - `/admin/employees` (`ManageEmployees.razor`): Employee roster management.
  - `/admin/users` (`ManageUsers.razor`): User account provisioning & System Administrator role modification.
  - `/admin/sections` (`ManageSections.razor`): Division-scoped organizational sections.
  - `/admin/system-access` (`ManageSystemAccess.razor`): System access overrides.
  - `/sysadmin/divisions` (`ManageDivisions.razor`): Global division directory.
  - `/sysadmin/systems` (`ManageSystems.razor`): Office system registration and URL management.
  - `/sysadmin/scopes` (`ManageAdminScopes.razor`): Division scopes and system management authority.
  - `/sysadmin/audit-logs` (`AuditLogs.razor`): Full audit log viewer.
  - `/sysadmin/login-histories` (`LoginHistories.razor`): Authentication event viewer.
- Role Management Governance:
  - Role changes strictly allowed only for System Administrators.
  - Normal Change Role dropdown exposes `USER` and `ADMINISTRATOR` roles only.
  - UI promotion to or demotion from `SYSTEM_ADMINISTRATOR` is strictly prohibited.
  - Self-role modification forbidden.
  - First System Administrator initial setup preserved as separate backend dependency (no setup UI created).
- Test Suites:
  - 54 new Step 5 unit tests in `DO.OneAccess.UnitTests`:
    - `JwtClaimsParsingTests` (decoding, claims mapping, array roles, anonymous & malformed token fallback).
    - `CustomAuthorizationMessageHandlerTests` (Bearer attachment, auth exclusion, single 401 retry, infinite loop prevention, logout on failure, concurrent 401 deduplication with SemaphoreSlim).
    - `ProblemDetailsReaderTests` (RFC 7807 title/detail, validation error dictionary parsing, non-JSON fallback).
    - `RouteAndAuthorizationGuardTests` (reflection validation of `[Authorize]` and `[Route]` attributes across all pages).
    - `TypedApiClientTests` (HTTP method, query strings, role payload, external launch URL security without JWT).
    - `IconComponentTests` (icon parameter defaults, known SVG icon verification, unknown icon safety).
  - All 140 solution tests pass (115 Unit Tests + 25 Integration Tests).
  - Zero database schema changes, zero migrations, zero database modifications.

---

## [1.5.0] — 2026-09-11 — Authorization & Access Control Implementation (Step 4)

### Added

- Application-layer exception hierarchy in `DO.OneAccess.Application.Common.Exceptions`:
  - `NotFoundException` (404), `ForbiddenException` (403), `ValidationException` (400), `ConflictException` (409).
- Application-layer authorization helper `AuthorizationHelper`:
  - Active actor database re-verification for state-mutating operations.
  - Division scope boundary validation.
  - System-management access validation (`AdministratorSystemAccess`).
  - System Administrator global access short-circuit.
- Application service interfaces and implementations:
  - `ISystemAccessService` / `SystemAccessService` (4-step access resolution, UPSERT `UserSystemAccess`, division & system-grant scoped queries).
  - `IAdminScopeService` / `AdminScopeService` (manage `AdministratorScopes` and `AdministratorSystemAccess`).
  - `IUserService` / `UserService` (user management, self-modification guards, role change restriction to SysAdmin).
  - `IEmployeeService` / `EmployeeService` (employee management with division/section scope enforcement).
  - `IDivisionService` / `DivisionService` (mutation restricted to SysAdmin, division-scoped read for Administrators).
  - `ISectionService` / `SectionService` (section management with division scope enforcement).
  - `ISystemService` / `SystemService` (system registry management, SysAdmin mutation, Administrator system grant checks).
  - `IAuditService` / `AuditService` (audit logging and query).
  - `ILoginHistoryService` / `LoginHistoryService` (login history query).
- Server authorization policies and infrastructure:
  - Named policies in `Program.cs`: `"SystemAdministratorOnly"`, `"AdministratorOrAbove"`, `"AnyAuthenticatedUser"`.
  - Global `ExceptionHandlingMiddleware` mapping domain/application exceptions to RFC 7807 `ProblemDetails`.
  - Base API controller `ApiControllerBase` with `GetActorUserId()` extraction from JWT claims.
- REST Controllers with coarse-grained policy decorators and fine-grained service calls:
  - `UsersController` (`/api/users`)
  - `EmployeesController` (`/api/employees`)
  - `DivisionsController` (`/api/divisions`)
  - `SectionsController` (`/api/sections`)
  - `SystemsController` (`/api/systems`)
  - `SystemAccessController` (`/api/system-access`)
  - `AdminScopesController` (`/api/admin-scopes`)
  - `AdminSystemAccessController` (`/api/admin-system-access`)
  - `AuditLogsController` (`/api/audit-logs`)
  - `LoginHistoriesController` (`/api/login-histories`)
- Unit test suites in `DO.OneAccess.UnitTests`:
  - `SystemAccessServiceTests` (4-step decision tree, UPSERT, inactive entity guards, scope boundaries).
  - `AdminScopeServiceTests` (division scope assignment, system access grants, duplicate conflicts, role restrictions).
  - `UserServiceAuthorizationTests` (privilege escalation guards, role change restrictions, self-modification prevention).
  - `SectionAndDivisionServiceTests` (cross-division 403 prevention, section division matching).
  - `SystemServiceAuthorizationTests` (system-management grant validation).
- Integration test suite in `DO.OneAccess.IntegrationTests`:
  - `AuthorizationApiIntegrationTests` (unauthenticated 401, role-based 403, cross-division 403, grant missing 403, happy path 200/201/204 against `DO_OneAccess_Test`).

---

## [1.4.0] — 2026-09-11 — Authentication Implementation (Step 3)

### Added

- `IPasswordHasher` / `PasswordHasher` using PBKDF2 with HMAC-SHA256 and configurable iterations.
- `IJwtTokenService` / `JwtTokenService` generating access tokens (15-min lifetime) and secure cryptographic refresh tokens (7-day lifetime, SHA-256 stored hash).
- `IAuthService` / `AuthService` handling login, token rotation, revocation, and login history auditing.
- Server `AuthController` with cookie-based refresh token handling, CSRF token mitigation, and public login endpoints.
- Integration tests in `AuthApiIntegrationTests` testing login, token refresh, and logout against `DO_OneAccess_Test`.

---

## [1.3.0] — 2026-09-10 — Database Foundation Implementation (Task 2)

### Added

- Scaffolding of Onion Architecture solution (`DO.OneAccess.sln`) with .NET 10 projects:
  - `src/DO.OneAccess.Domain`
  - `src/DO.OneAccess.Application`
  - `src/DO.OneAccess.Infrastructure`
  - `src/DO.OneAccess.Server` (minimal host for EF Core tooling/DI)
  - `src/DO.OneAccess.Client` (Blazor WebAssembly client scaffold)
  - `tests/DO.OneAccess.UnitTests`
  - `tests/DO.OneAccess.IntegrationTests`
- 13 canonical domain entities in `DO.OneAccess.Domain`:
  - `Role`, `Division`, `Section`, `Employee`, `User`, `System`, `SystemSetting`, `UserSystemAccess`, `AdministratorScope`, `AdministratorSystemAccess`, `RefreshToken`, `LoginHistory`, `AuditLog`.
- Domain enums: `RoleType`, `DefaultAccess`, `AccessOverride`.
- `IApplicationDbContext` persistence abstraction in `DO.OneAccess.Application`.
- `AppDbContext` and 13 explicit `IEntityTypeConfiguration<T>` implementations in `DO.OneAccess.Infrastructure/Persistence/Configurations/`.
- Tiered identifier strategy enforced: GUIDs for integration identities, integers for internal references, and `bigint` for high-volume logs and access records.
- Foreign key delete behavior configured to `DeleteBehavior.Restrict` / `NoAction` to prevent unintended cascade deletes.
- CreatedBy/UpdatedBy audit columns implemented as nullable `uniqueidentifier` (`Guid?`) without database-level FKs to prevent circular dependency traps during initial system bootstrap.
- Deterministic initial EF Core migration `20260911030348_InitialCreate` generating all 13 tables, keys, constraints, and indexes.
- Seed data configured via EF Core `HasData` for exactly the 3 canonical roles:
  - 1 = `SYSTEM_ADMINISTRATOR` (System Administrator)
  - 2 = `ADMINISTRATOR` (Administrator)
  - 3 = `USER` (User)
- Unit test suite (`ModelBuilderTests`) verifying model creation, 13 tables, tiered primary keys, unique constraints, foreign keys, delete behaviors, and seed data.
- Integration test suite (`DatabaseMigrationTests`) validating schema migration, table creation, foreign keys, unique indexes, and seed data against SQL Server Express (`localhost\SQLEXPRESS`) with strict test database isolation (`DO_OneAccess_Test`).
- Strict task boundary maintained: zero JWT, zero login endpoints, zero authentication middleware, zero authorization handlers, and zero UI pages.

---

## [1.2.0] — 2026-09-10 — Schema Naming Correction (Task 1.3)

### Changed

- Normalized `AdministratorScopes` foreign key column to `AdministratorUserId` (`uniqueidentifier`) referencing `Users.UserId`, aligning with `AdministratorSystemAccess.AdministratorUserId`.
- Enforced constraint `UNIQUE(AdministratorUserId)` across documentation and ERD diagrams.
- Reconfirmed canonical `LoginHistories` definition (`Success`, `CreatedAt`).
- Adherence to No Implementation Code Rule: zero code, migrations, or database assets created.

---

## [1.1.0] — 2026-09-10 — Specification v1.1 Documentation Normalization (Task 1.2)

### Changed

- Normalized all documentation across `/docs` to align strictly with `docs/DO.OneAccess-Specification-v1.md` as the sole canonical source of truth.
- Established **Tiered Identifier Strategy**:
  - GUIDs (`uniqueidentifier` / `Guid`) scoped strictly to external/integration entities (`EmployeeId`, `UserId`, `SystemId`).
  - Numeric identifiers for internal reference tables: `RoleId` (`smallint`), `DivisionId` & `SectionId` (`int`), `AdministratorScopeId`, `AdministratorSystemAccessId`, `UserSystemAccessId`, `RefreshTokenId`, `LoginHistoryId`, `AuditLogId`, `SystemSettingId` (`bigint`).
- Enforced explicit entity primary key column naming across all 13 baseline tables (never generic `Id`).
- Enforced canonical column names:
  - `SystemSettings`: `SystemSettingId`, `SystemId`, `SettingKey`, `SettingValue`, `IsSecret`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` (never generic `Key`/`Value`).
  - `Systems`: `SystemId`, `SystemCode`, `SystemName`, `Description`, `BaseUrl`, `IconUrl`, `DefaultAccess`, `IsActive`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` (never generic `Code`/`Name`).
- Clarified canonical roles: `SYSTEM_ADMINISTRATOR`, `ADMINISTRATOR`, `USER` (codes vs display names).
- Formalized Section uniqueness: `UNIQUE(DivisionId, Code)`.
- Normalized `UserSystemAccess`: explicit PK `UserSystemAccessId` (`bigint`), `AccessType` (`Allow`/`Deny`), full audit fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`).
- Normalized `AdministratorSystemAccess`: explicit PK `AdministratorSystemAccessId` (`bigint`), `AdministratorUserId`, composite `UNIQUE(AdministratorUserId, SystemId)`.
- Normalized `AuditLogs`: explicit PK `AuditLogId` (`bigint`), actor `UserId` as nullable GUID, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `CreatedAt`.
- Normalized `RefreshTokens`: explicit PK `RefreshTokenId` (`bigint`), token hash only, revocation tracked via `RevokedAt` timestamp and `ReplacedByTokenId` (`bigint`).
- Standardized audit actor convention: `CreatedBy` / `UpdatedBy` as `uniqueidentifier` referencing `Users.UserId` (nullable on system-created/seed records).

### Notes

- Strict adherence to the No Implementation Code Rule: zero `.cs`, `.razor`, `.sql`, or migrations created.

---

## [1.0.0] — 2026-09-10 — Documentation Foundation

### Added

- `docs/DO.OneAccess-Specification-v1.md` — Master specification; primary source of truth.
- `docs/System-Architecture/Overview.md` — High-level system overview.
- `docs/System-Architecture/Architecture.md` — Onion Architecture layer definitions and dependency direction.
- `docs/System-Architecture/Project-Structure.md` — Solution and project folder layout.
- `docs/System-Architecture/Database-Architecture.md` — Database design at the system architecture level.
- `docs/System-Architecture/ERD.md` — Full entity relationship diagram (Mermaid).
- `docs/System-Architecture/Integration-Architecture.md` — Integration boundaries and future direction.
- `docs/Security/Security-Overview.md` — Security philosophy and domain coverage.
- `docs/Security/Authentication.md` — Authentication flow, token pair, refresh, and login history.
- `docs/Security/JWT.md` — JWT claims, lifetime, signing, and validation.
- `docs/Security/Authorization.md` — Role-based and scope-based authorization model.
- `docs/Security/Security-Guidelines.md` — Developer security checklist and rules.
- `docs/Access-Control/Overview.md` — Two-dimension access control model.
- `docs/Access-Control/Roles.md` — Three-role definitions with capabilities and restrictions.
- `docs/Access-Control/Administrator-Scope.md` — Administrator Division scope and system management scope.
- `docs/Access-Control/System-Access.md` — System DefaultAccess policy and UserSystemAccess overrides.
- `docs/Access-Control/Access-Rules.md` — Full access resolution flow and audit requirements.
- `docs/Frontend/Overview.md` — Frontend technology and responsibilities.
- `docs/Frontend/Architecture.md` — Blazor WASM architecture, layers, and token handling.
- `docs/Frontend/Project-Structure.md` — Client project folder layout.
- `docs/Frontend/UI-Design.md` — Design principles, layouts, and key pages.
- `docs/Frontend/Navigation.md` — Routes, route guards, and redirect behavior.
- `docs/Frontend/Authentication-Flow.md` — Client-side login, refresh, and logout flows.
- `docs/Frontend/Authorization.md` — Blazor authorization mechanisms and page-role matrix.
- `docs/Frontend/State-Management.md` — Auth state, token storage, and state flows.
- `docs/Frontend/API-Integration.md` — HttpClient setup, auth handler, and service pattern.
- `docs/Frontend/Component-Guidelines.md` — Component design principles and patterns.
- `docs/Backend/Overview.md` — Backend project responsibilities.
- `docs/Backend/Architecture.md` — Request pipeline, controller design, service/repository pattern.
- `docs/Backend/API-Design.md` — All endpoint groups, roles, and HTTP conventions.
- `docs/Backend/Application-Layer.md` — Service interfaces, scope enforcement, audit logging.
- `docs/Backend/Validation.md` — Validation strategy and FluentValidation approach.
- `docs/Database/Schema.md` — Schema design principles and table inventory.
- `docs/Database/Tables.md` — Column definitions for all 12 tables.
- `docs/Database/Relationships.md` — FK relationships, cardinality, and cascade behavior.
- `docs/Database/Indexes.md` — Index definitions and rationale.
- `docs/Database/Migration-Strategy.md` — EF Core migration principles and application process.
- `docs/Database/Seed-Data.md` — Roles seeding and initialization rules.
- `docs/Deployment/Requirements.md` — Runtime, build, and infrastructure requirements.
- `docs/Deployment/Development.md` — Development environment setup steps.
- `docs/Deployment/Staging.md` — Staging environment placeholder.
- `docs/Deployment/Production.md` — Production environment placeholder.
- `docs/Integration/Overview.md` — Integration philosophy and v1 scope.
- `docs/Integration/Integration-Contract.md` — Integration identifiers and stability guarantees.
- `docs/Integration/Identity.md` — GUID identity, stability rules, and future use cases.
- `docs/Integration/Future-SSO.md` — SSO direction (out of v1 scope).
- `docs/Updates/Update-Process.md` — Change categories and documentation rule.
- `docs/Updates/Database-Migrations.md` — Migration creation and application process.
- `docs/Updates/Release-Process.md` — Release process placeholder.
- `docs/Updates/Changelog.md` — This file.
- `README.md` — Project README with full documentation index.

### Notes

- No application code implemented in this version.
- No database migrations created.
- No secrets introduced.
- Documentation foundation only.

---

*Format based on [Keep a Changelog](https://keepachangelog.com/). Versions use Semantic Versioning.*
