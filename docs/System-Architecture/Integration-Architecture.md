# System Architecture — Integration Architecture

> **Section:** System Architecture  
> **Document:** Integration Architecture  
> **Source of Truth:** [Master Specification](../DO.OneAccess-Specification-v1.md)  
> **Related:** [../Integration/Overview.md](../Integration/Overview.md) | [../Integration/Identity.md](../Integration/Identity.md)

---

## Integration Philosophy

DO.OneAccess v1 does **not** implement a full SSO, OIDC, or OAuth server. However, the system is deliberately designed to remain **integration-ready** so that future office systems can reference DO.OneAccess identity without requiring a major re-architecture.

---

## Stable Integration Identifiers

| Identifier | Type | Purpose |
|---|---|---|
| `UserId` | GUID | Stable identity for a DO.OneAccess user account; used by future integrations |
| `SystemId` | GUID | Stable identity for a registered system |
| `EmployeeNumber` | string | Official business identifier; unique per employee |

> **Rule:** Future office systems that integrate with DO.OneAccess must reference `UserId` and `SystemId`. Do not use usernames or email addresses as integration keys.

---

## v1 Integration Boundary

In v1, DO.OneAccess itself is the consumer of its own identity system. The integration boundary is limited to:

- Internal API calls from the Blazor WebAssembly client to the ASP.NET Core API.
- JWT access tokens issued by DO.OneAccess API and validated by the same API.

No external systems consume DO.OneAccess tokens in v1.

---

## Future Integration Direction

```
┌─────────────────────────────────┐
│      Future Office System A     │
│   References DO.OneAccess       │
│   UserId + SystemId             │
└──────────────┬──────────────────┘
               │  UserId / SystemId reference
               ▼
┌─────────────────────────────────┐
│        DO.OneAccess API         │
│   Authoritative Identity Source │
└─────────────────────────────────┘
```

Future integration patterns that DO.OneAccess is designed to support:

1. **Identity lookup** — A future system calls DO.OneAccess API to verify a `UserId` and retrieve basic profile.
2. **Access verification** — A future system calls DO.OneAccess API to confirm a user has access to a specific system by `SystemId`.
3. **SSO/OIDC** — If explicitly requested in a future version, DO.OneAccess could expose an OIDC-compatible token endpoint. This is out of scope for v1.

---

## Integration Readiness Rules

| Rule | Detail |
|---|---|
| Use GUID for Integration Entities | Externally meaningful and integration-facing identities (`UserId`, `SystemId`) are GUIDs; safe for cross-system reference |
| Explicit PK Naming | Primary keys use entity-specific names (`UserId`, `SystemId`, etc.); generic `Id` is not used |
| Stable Identifiers | `UserId` is the stable reference; usernames/emails can change; `EmployeeNumber` is business identity |
| `SystemId` is stable | Systems must not be deleted without considering referential impact on integrating systems |
| Soft delete important entities | `Users`, `Systems`, `Divisions`, `Sections` are soft-deleted to preserve referential integrity |

---

## What is NOT in v1

- No OIDC Authorization Server.
- No OAuth 2.0 flows.
- No external identity provider federation (e.g., Azure AD, Okta).
- No API key management for external consumers.
- No webhook/event publishing.

These capabilities may be added in future versions if explicitly requested and approved.

---

## Related Documents

| Document | Link |
|---|---|
| Integration Overview | [../Integration/Overview.md](../Integration/Overview.md) |
| Integration Contract | [../Integration/Integration-Contract.md](../Integration/Integration-Contract.md) |
| Identity | [../Integration/Identity.md](../Integration/Identity.md) |
| Future SSO | [../Integration/Future-SSO.md](../Integration/Future-SSO.md) |
