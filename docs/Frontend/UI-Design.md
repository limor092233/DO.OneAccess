# Frontend — UI Design

> **Section:** Frontend  
> **Document:** UI Design  
> **Related:** [Overview.md](Overview.md) | [Navigation.md](Navigation.md) | [Component-Guidelines.md](Component-Guidelines.md)

---

## Design Principles

| Principle | Application |
|---|---|
| Clarity | Users immediately see what systems they can access |
| Role-appropriate | UI adapts to show relevant controls per role; no clutter |
| Consistent | Shared layout, components, and patterns across all pages |
| Accessible | Semantic HTML; keyboard navigable; sufficient color contrast |
| Responsive | Functional on desktop and tablet viewports |

---

## Application Layouts

### Unauthenticated Layout
Used for the Login page. Minimal layout with the portal branding centered.

### Authenticated Layout (Main Layout)
Used for all authenticated pages. Contains:
- **Top bar** — Portal name, authenticated user info, logout button.
- **Side navigation** — Role-appropriate navigation menu.
- **Content area** — Page-specific content.

---

## Key Page Designs

### Login Page
- Username input field (associated employee username).
- Password input field.
- "Sign In" button with loading spinner state.
- Error message area for non-sensitive error display.
- Silent session restoration on initial load via HttpOnly refresh cookie.

### Registered Systems Portal (`/systems`)
- Primary portal landing page for all authenticated users (`USER`, `ADMINISTRATOR`, `SYSTEM_ADMINISTRATOR`).
- Replaces Dashboard in visible navigation (`/dashboard` retained as an authenticated compatibility route).
- Greeting with the user's username.
- Responsive grid of accessible systems (tiles/cards).
- Each system card displays the system name, code, description, and configured icon.
- "Launch Application" button opens the backend-configured trusted `BaseUrl` in a new tab (`target="_blank" rel="noopener noreferrer"`).
- **Security Guarantee:** No JWT or access token is ever appended to the external system launch URL, query string, or hash fragment.

### Sidebar Navigation Structure
- **PORTAL**
  - **Registered Systems** (`/systems`): All authenticated users (`USER`, `ADMINISTRATOR`, `SYSTEM_ADMINISTRATOR`).
- **ADMINISTRATION** (`ADMINISTRATOR` and `SYSTEM_ADMINISTRATOR`):
  - **Manage Users** (`/admin/users`): Unified management of user accounts and employee profiles. Create User modal features dependent Division → Section selection (User requires valid Section under selected Division; Administrator requires assigned Division, with Section fixed to null). System Administrator creation is removed. System Administrator Role Transfer is available strictly to the active System Administrator via an explicit transfer modal requiring target selection, successor resulting role assignment (Administrator with Division or User with Division + Section), and explicit confirmation.
  - **Manage Division** (`/sysadmin/divisions` for System Administrator managing all Divisions and expandable child Sections; `/admin/divisions` for Administrator displaying assigned Division profile and child Sections).
  - **System Access** (`/admin/system-access`): System override allow/deny grants.
- **SYSTEM ADMINISTRATION** (`SYSTEM_ADMINISTRATOR` only):
  - **Admin Scopes** (`/sysadmin/scopes`): Assign division scope and system management grants.
  - **Audit Logs** (`/sysadmin/audit-logs`): Full audit log viewer.
  - **Login History** (`/sysadmin/login-histories`): Authentication event viewer.

### Role Management UI Rules
- Role changes are restricted strictly to `SYSTEM_ADMINISTRATOR`.
- The Change Role dropdown exposes **USER** and **ADMINISTRATOR** roles only.
- Direct promotion to or demotion from `SYSTEM_ADMINISTRATOR` is never permitted via standard role UI.
- Self-role modification is forbidden.
- The application guarantees the exactly-one System Administrator invariant for supported application workflows by restricting SysAdmin assignment to First Run Setup and the atomic Role Transfer workflow, protected by a Serializable transaction and post-condition validation.
- Initial System Administrator provisioning is performed via dedicated First Run Setup wizard (`/setup`), accessible only on an uninitialized deployment and guarded by a one-time bootstrap token.

### First Run Setup Wizard (`/setup`)
- Clean, focused card interface with DO.OneAccess platform branding.
- **Section 1 (Bootstrap Authorization):** One-time bootstrap token input from host console stdout or environment variable.
- **Section 2 (Employee Identity):** Employee number, First/Middle/Last names, official email, and optional position.
- **Section 3 (Credentials & Role):** System Administrator username, password, confirmation, and locked role badge (`SYSTEM_ADMINISTRATOR`).
- **Interactive States:** Loading status check, validation errors (per-field and banner), submitting spinner, and completion screen directing to `/login?initialized=true`.

## Component Structure Pattern

```
Page
 └── Layout (MainLayout)
       ├── TopBar
       ├── NavMenu (role-driven)
       └── Content
             └── Page-specific components
```

---

## Error and Loading States

All data-loading UI must handle:
- **Loading state** — Show a spinner or skeleton while awaiting API response.
- **Error state** — Show a clear, non-sensitive error message. Do not expose internal error details.
- **Empty state** — Show a meaningful message when a list is empty.

---

## Color and Theme

- The portal uses a consistent color palette established in `wwwroot/css/app.css`.
- The design system is defined in shared CSS; components do not use inline styles for theming.
- Exact color palette and typography to be defined during implementation.

---

## Accessibility Notes

- All interactive elements must have accessible labels.
- Form inputs must have associated labels.
- Error messages must be associated with their fields using `aria-describedby`.
- Color must not be the only indicator of state.

---

## Related Documents

| Document | Link |
|---|---|
| Navigation | [Navigation.md](Navigation.md) |
| Component Guidelines | [Component-Guidelines.md](Component-Guidelines.md) |
| Frontend Architecture | [Architecture.md](Architecture.md) |
