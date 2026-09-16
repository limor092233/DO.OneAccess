# Integration — Future SSO

> **Section:** Integration  
> **Document:** Future SSO Direction  
> **Status:** Out of scope for v1 — Direction document only  
> **Related:** [Overview.md](Overview.md) | [Integration-Contract.md](Integration-Contract.md)

---

## v1 Position

DO.OneAccess v1 does **not** implement SSO, OIDC, or OAuth. These are explicitly out of scope for v1 to avoid over-engineering. The system is designed to remain integration-ready without building this infrastructure prematurely.

---

## What "Integration-Ready" Means for SSO

The following design decisions in v1 enable a future SSO migration without full re-architecture:

| Decision | SSO Benefit |
|---|---|
| GUID `UserId` as stable identity | OIDC `sub` claim can be the DO.OneAccess `UserId` |
| JWT-based auth already in place | JWT infrastructure is already present; extending to OIDC is an incremental step |
| No email/username as integration key | Avoids coupling integrations to mutable user attributes |
| Server-side authorization | Authorization logic remains server-side; future OIDC tokens would still be validated here |

---

## Possible Future SSO Architecture

If SSO is explicitly requested in a future version, options include:

### Option A: DO.OneAccess as OIDC Provider
- DO.OneAccess exposes an OIDC Authorization Server.
- External office systems register as OIDC relying parties.
- Users authenticate once at DO.OneAccess; external systems receive ID tokens.
- This requires significant additional engineering (OIDC discovery endpoint, authorization code flow, etc.).

### Option B: External Identity Provider Integration
- Integrate with an external Identity Provider (e.g., Azure Active Directory, Okta).
- DO.OneAccess federates identity to the external IdP.
- This requires mapping external identities to DO.OneAccess `UserId`.

### Option C: Machine-to-Machine API
- External systems call a DO.OneAccess API to verify identity and access.
- No browser-based SSO flow required.
- Simpler to implement; suitable if full browser SSO is not needed.

---

## Decision Rule

> **No SSO architecture must be implemented in v1.** Any future SSO work requires an explicit requirement, a separate architecture decision, and documentation updates before implementation begins.

---

## Related Documents

| Document | Link |
|---|---|
| Integration Overview | [Overview.md](Overview.md) |
| Integration Contract | [Integration-Contract.md](Integration-Contract.md) |
| Identity | [Identity.md](Identity.md) |
| Integration Architecture | [../System-Architecture/Integration-Architecture.md](../System-Architecture/Integration-Architecture.md) |
