# Access Control — Roles

> **Section:** Access Control  
> **Document:** Roles  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [Overview.md](Overview.md) | [Administrator-Scope.md](Administrator-Scope.md)

---

## Role Definitions

DO.OneAccess has exactly **three** roles at launch. Each User is assigned exactly one role (`Users.RoleId`).

> **Semantic Distinction Rule:** `Code` and `Name` are distinct and must not be treated as interchangeable. `Code` is the immutable programmatic identifier (used in authorization checks and constant definitions), while `Name` is the human-readable display label.

---

### 1. System Administrator

| Property | Value |
|---|---|
| RoleId (`smallint`) | `1` |
| Code | `SYSTEM_ADMINISTRATOR` |
| Name | `System Administrator` |
| Authority | Global |
| Division restriction | None |
| Section restriction | None |

**Capabilities:**

| Capability | Allowed |
|---|---|
| Manage all Users | ✅ |
| Manage all Administrators | ✅ |
| Manage Divisions and Sections | ✅ |
| Manage registered Systems | ✅ |
| Manage system access (all users) | ✅ |
| Manage Administrator scopes | ✅ |
| Review Audit Logs | ✅ |
| Review Login Histories | ✅ |
| Global scope | ✅ |
| Division / Section assignment required | ❌ Not required |
| Self-register as additional System Admin | ❌ Not permitted |

**Special Rules:**
- The first System Administrator is created through a dedicated one-time secure setup gateway (`POST /api/setup` and client `/setup`) protected by a one-time cryptographic bootstrap token.
- Initial setup dynamically checks for any SysAdmin record (`Users.Any(u => u.RoleId == 1)`). Once initialized, the setup gateway is permanently locked (409 Conflict), even if the SysAdmin is deactivated.
- **Exactly-One System Administrator Invariant:** The application guarantees the exactly-one System Administrator invariant for supported application workflows by restricting SysAdmin assignment to First Run Setup and the atomic Role Transfer workflow, protected by a Serializable transaction and post-condition validation. Additional System Administrators cannot be created, self-registered, or assigned through general role management.
- **Role Transfer Workflow:** The active System Administrator can transfer universal platform authority to an active User or Administrator via `POST /api/users/system-administrator/transfer`. The target account receives `RoleId = 1`, `SectionId = null`, and any prior `AdministratorScopes` or `AdministratorSystemAccess` are removed. The departing System Administrator transitions to a valid `ADMINISTRATOR` (with assigned `DivisionId`) or `USER` (with assigned `DivisionId` and child `SectionId`). All existing active refresh tokens for both users are immediately revoked, forcing clean session renewal. The transaction is atomic under `IsolationLevel.Serializable` with post-condition check `COUNT(Users WHERE RoleId == 1) == 1`. Historical audit logs and login histories are fully preserved.

---

### 2. Administrator

| Property | Value |
|---|---|
| RoleId (`smallint`) | `2` |
| Code | `ADMINISTRATOR` |
| Name | `Administrator` |
| Authority | Scoped to one assigned Division |
| Division restriction | Exactly one Division (via `AdministratorScopes`) |

**Capabilities:**

| Capability | Allowed |
|---|---|
| Manage their assigned Division | ✅ |
| Manage Sections within their Division | ✅ |
| Manage Users within their Division | ✅ |
| Manage Systems granted to them by System Administrator | ✅ |
| Manage another Division | ❌ Not permitted |
| Grant access outside their organizational scope | ❌ Not permitted |
| Grant access outside their system-management scope | ❌ Not permitted |
| Elevate their own role | ❌ Not permitted |
| Elevate other Users to Administrator or System Administrator | ❌ Not permitted |

**Scope enforcement:**
- Division scope is enforced by the API on every administrative action.
- System-management scope is enforced by the API when an Administrator attempts to manage system access.

---

### 3. User

| Property | Value |
|---|---|
| RoleId (`smallint`) | `3` |
| Code | `USER` |
| Name | `User` |
| Authority | Personal access only |
| Division / Section | Assigned via `User.SectionId` → Section → Division |

**Capabilities:**

| Capability | Allowed |
|---|---|
| Login | ✅ |
| View accessible systems | ✅ |
| Access systems per access rules | ✅ |
| Manage users | ❌ Not permitted |
| Manage roles | ❌ Not permitted |
| Manage systems | ❌ Not permitted |
| Manage permissions | ❌ Not permitted |

---

## Role Assignment Rules

| Rule | Detail |
|---|---|
| One role per user | A User has exactly one role at any time |
| Role changes authority | Only the System Administrator can change a User's role (`PUT /api/users/{userId}/role`); Administrators cannot change roles |
| Permitted UI transitions | System Administrators can change roles between `USER` and `ADMINISTRATOR` only |
| Normal UI restriction | Promotion to or demotion from `SYSTEM_ADMINISTRATOR` is prohibited through normal UI |
| Self-role modification | Never permitted for any role (System Administrators cannot modify their own role) |
| Initial bootstrap | Initial System Administrator is provisioned via secure first-run setup gateway (`POST /api/setup` / `/setup`) with single-use bootstrap token |

---

## Database Representation

Roles are stored in the `Roles` table with a stable set of predefined entries seeded at initialization:

| RoleId (`smallint`) | Code | Name | Description | IsActive |
|---|---|---|---|---|
| 1 | `SYSTEM_ADMINISTRATOR` | System Administrator | Global platform administrator | `true` |
| 2 | `ADMINISTRATOR` | Administrator | Division-scoped administrator | `true` |
| 3 | `USER` | User | Regular employee user | `true` |

Role records are reference data and must not be deleted.

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [../DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Access Control Overview | [Overview.md](Overview.md) |
| Administrator Scope | [Administrator-Scope.md](Administrator-Scope.md) |
| Authorization | [../Security/Authorization.md](../Security/Authorization.md) |
| Seed Data | [../Database/Seed-Data.md](../Database/Seed-Data.md) |
