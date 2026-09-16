# Integration — Overview

> **Section:** Integration  
> **Document:** Overview  
> **Related:** [Integration-Contract.md](Integration-Contract.md) | [Identity.md](Identity.md) | [Future-SSO.md](Future-SSO.md) | [../System-Architecture/Integration-Architecture.md](../System-Architecture/Integration-Architecture.md)

---

## Integration Philosophy

DO.OneAccess v1 is an **internal-facing** authentication and access-control system. It does not expose an external integration API in v1. However, the system is deliberately designed to remain **integration-ready** so that future office systems can reference DO.OneAccess identity without requiring a major re-architecture.

---

## v1 Integration Scope

In v1:
- DO.OneAccess is a self-contained system.
- The only "integration" is the Blazor WASM Client calling the ASP.NET Core API.
- No external systems consume DO.OneAccess tokens in v1.

---

## Future Integration Possibilities

Future office systems may need to:

1. **Verify a user's identity** — Confirm that a `UserId` belongs to a real, active DO.OneAccess user.
2. **Check system access** — Confirm that a user has access to a specific system by `SystemId`.
3. **Retrieve basic user profile** — Retrieve name, role, or organizational unit for display in an integrated system.
4. **Single Sign-On** — If requested, DO.OneAccess could issue tokens usable by other systems (requires explicit future architecture work).

---

## Integration Design Rules

| Rule | Detail |
|---|---|
| Use `UserId` | Future systems reference DO.OneAccess `UserId` (GUID) as the user identity |
| Use `SystemId` | Future systems reference DO.OneAccess `SystemId` (GUID) as the system identity |
| No username/email as identity | Usernames and emails can change; `UserId` is stable |
| No SSO in v1 | Full SSO/OIDC/OAuth is not implemented in v1 |
| No external API keys | No API key management for external consumers in v1 |
| Soft delete for stability | `Users` and `Systems` are soft-deleted to preserve referential integrity for integrating systems |

---

## Related Documents

| Document | Link |
|---|---|
| Integration Contract | [Integration-Contract.md](Integration-Contract.md) |
| Identity | [Identity.md](Identity.md) |
| Future SSO | [Future-SSO.md](Future-SSO.md) |
| Integration Architecture | [../System-Architecture/Integration-Architecture.md](../System-Architecture/Integration-Architecture.md) |
