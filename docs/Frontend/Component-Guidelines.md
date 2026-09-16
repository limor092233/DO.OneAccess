# Frontend — Component Guidelines

> **Section:** Frontend  
> **Document:** Component Guidelines  
> **Related:** [Architecture.md](Architecture.md) | [UI-Design.md](UI-Design.md) | [Project-Structure.md](Project-Structure.md)

---

## Component Design Principles

| Principle | Rule |
|---|---|
| Single Responsibility | Each component does one thing well |
| Reusability | Shared components live in `Components/Shared/` |
| No Direct API Calls | Components call Services; Services call the API |
| No Business Logic | Business logic belongs in Services or the Application layer |
| No Hardcoded Strings | Use constants or resource files for user-facing text |
| Authorization | Use `<AuthorizeView>` or `[Authorize]`; never bypass with custom checks |

---

## Component Categories

### Page Components (`Pages/`)
- Routable via `@page "/route"`.
- Responsible for page-level state and orchestrating child components.
- Inject services directly.
- Handle loading, error, and empty states.

### Layout Components (`Components/Layout/`)
- `MainLayout.razor` — Shell layout for authenticated pages.
- `NavMenu.razor` — Role-driven navigation sidebar.
- `TopBar.razor` — Portal branding and user info.

### Shared Components (`Components/Shared/`)
- `LoadingSpinner.razor` — Displayed while awaiting API responses.
- `ErrorMessage.razor` — Displays error messages.
- `ConfirmDialog.razor` — Reusable confirmation modal for destructive actions.

---

## Component Lifecycle Pattern

For data-loading pages:

```razor
@code {
    private List<SomeDto>? _items;
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _items = await SomeService.GetItemsAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = "Failed to load data. Please try again.";
            // Log ex appropriately; do not expose it to the UI
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

---

## Error Handling in Components

- Always catch exceptions from service calls.
- Display a generic, user-friendly error message.
- Do not expose exception messages, stack traces, or internal details in the UI.
- Log errors appropriately (client-side logging or surface to server for audit).

---

## Form Handling

- Use Blazor's `<EditForm>` with component-level validation or `DataAnnotationsValidator`.
- Disable the submit button while a request is in flight (`_isSubmitting` flag).
- Clear error messages before re-submission.
- Show field-level validation errors using `<ValidationMessage>`.

---

## Destructive Actions

For actions that delete or deactivate records:
- Show a `ConfirmDialog` before proceeding.
- Clearly state what will happen.
- Require explicit confirmation.

---

## Authorization in Components

Use Blazor's built-in authorization:

```razor
<AuthorizeView Roles="SystemAdministrator">
    <Authorized>
        <button @onclick="DeleteDivision">Delete Division</button>
    </Authorized>
</AuthorizeView>
```

Do not implement custom role-check logic that bypasses Blazor's auth framework.

---

## Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Component files | PascalCase | `ManageUsers.razor` |
| Private fields | `_camelCase` | `_isLoading`, `_users` |
| Event handlers | `On` + PascalCase | `OnSaveClicked` |
| Parameters | PascalCase | `[Parameter] public string Title { get; set; }` |

---

## Related Documents

| Document | Link |
|---|---|
| Frontend Architecture | [Architecture.md](Architecture.md) |
| UI Design | [UI-Design.md](UI-Design.md) |
| Project Structure | [Project-Structure.md](Project-Structure.md) |
| Frontend Authorization | [Authorization.md](Authorization.md) |
