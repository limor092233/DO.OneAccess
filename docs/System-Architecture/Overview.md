# System Architecture — Overview

> **Section:** System Architecture  
> **Document:** Overview  
> **Related:** [Architecture.md](Architecture.md) | [Project-Structure.md](Project-Structure.md)

---

## Purpose

DO.OneAccess is built as a secure, maintainable, and integration-ready centralized authentication and system-access portal. This section describes the system at a high level before deeper architectural documentation in subsequent files.

---

## High-Level System Description

DO.OneAccess provides:

1. **Authentication** — Employees authenticate using credentials. The system issues a short-lived JWT access token and a server-side hashed refresh token.
2. **System Access Resolution** — Upon login, the portal resolves which office systems the authenticated user is authorized to access.
3. **Administrative Management** — Administrators manage their scoped Division, Sections, and Users. System Administrators manage the entire platform.
4. **Audit Trail** — All significant access and administrative actions are recorded.

---

## Deployment Topology (Logical)

```
┌──────────────────────────────────────┐
│         Browser (Employee)           │
│    Blazor WebAssembly Client App     │
└──────────────┬───────────────────────┘
               │ HTTPS / JWT
               ▼
┌──────────────────────────────────────┐
│      DO.OneAccess.Server             │
│    ASP.NET Core Web API              │
│    - JWT issuance & validation       │
│    - Authorization enforcement       │
│    - REST API endpoints              │
└──────────────┬───────────────────────┘
               │ EF Core
               ▼
┌──────────────────────────────────────┐
│          SQL Server Database         │
└──────────────────────────────────────┘
```

---

## Layered Architecture Summary

The system follows Onion Architecture:

```
┌─────────────────────────────────────────┐
│              Domain Layer               │  ← Core entities, rules, interfaces
├─────────────────────────────────────────┤
│           Application Layer             │  ← Use cases, service contracts, DTOs
├─────────────────────────────────────────┤
│         Infrastructure Layer            │  ← EF Core, DB, external concerns
├─────────────────────────────────────────┤
│       Server Layer (API Host)           │  ← HTTP, JWT, controllers
├─────────────────────────────────────────┤
│       Client Layer (Blazor WASM)        │  ← SPA, UI, API calls
└─────────────────────────────────────────┘
```

See [Architecture.md](Architecture.md) for full details.

---

## Key Design Principles

| Principle | Application |
|---|---|
| Separation of concerns | Onion Architecture layer boundaries |
| Defense in depth | Authorization enforced server-side; UI hiding is not security |
| Least privilege | Administrators scoped to one Division; Users see only permitted systems |
| Auditability | Login and administrative actions are logged |
| Integration readiness | `UserId` and `SystemId` as stable GUIDs for future system references |
| No over-engineering | No full SSO/OIDC in v1; no unnecessary permission tables |

---

## Related Documents

| Document | Link |
|---|---|
| Master Specification | [DO.OneAccess-Specification-v1.md](../DO.OneAccess-Specification-v1.md) |
| Architecture Detail | [Architecture.md](Architecture.md) |
| Project Structure | [Project-Structure.md](Project-Structure.md) |
| ERD | [ERD.md](ERD.md) |
| Security Overview | [../Security/Security-Overview.md](../Security/Security-Overview.md) |
