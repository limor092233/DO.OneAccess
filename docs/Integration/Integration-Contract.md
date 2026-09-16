# Integration — Integration Contract

> **Section:** Integration  
> **Document:** Integration Contract  
> **Status:** v1 — Internal Only; External contract TBD in future version  
> **Related:** [Identity.md](Identity.md) | [Overview.md](Overview.md)

---

## v1 Contract Scope

In v1, DO.OneAccess does not expose a public integration API. The integration contract in this document defines what future integrating systems should expect from DO.OneAccess and what they must provide.

---

## What DO.OneAccess Provides

| Artifact | Description |
|---|---|
| `UserId` | Stable GUID identity for each user account |
| `SystemId` | Stable GUID identity for each registered system |
| `EmployeeNumber` | Official business identifier (unique per employee) |

---

## What Integrating Systems Should Use

| Scenario | Use | Do Not Use |
|---|---|---|
| Reference a user in another system | `UserId` (GUID) | Username, email, EmployeeNumber |
| Reference a system in DO.OneAccess | `SystemId` (GUID) | System name, code |
| Business-facing user identifier | `EmployeeNumber` | Internal IDs |

---

## Stability Guarantees

| Identifier | Stability |
|---|---|
| `UserId` | Stable for the lifetime of the user account; does not change |
| `SystemId` | Stable for the lifetime of the system record; does not change |
| `EmployeeNumber` | Stable business identifier; should not change |
| Username / Email | May change; must not be used as integration keys |

---

## Future Integration API (Placeholder)

If explicitly requested in a future version, DO.OneAccess may expose:

- `GET /api/integration/users/{userId}` — Verify user existence and retrieve basic profile.
- `GET /api/integration/access-check?userId={userId}&systemId={systemId}` — Verify if a user has access to a system.

These endpoints would require appropriate authentication (e.g., API key or machine-to-machine token). They are **not** in v1 scope.

---

## Related Documents

| Document | Link |
|---|---|
| Overview | [Overview.md](Overview.md) |
| Identity | [Identity.md](Identity.md) |
| Future SSO | [Future-SSO.md](Future-SSO.md) |
