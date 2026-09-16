# System Architecture — Project Structure

> **Section:** System Architecture  
> **Document:** Project Structure  
> **Related:** [Architecture.md](Architecture.md)

---

## Repository Layout

```
DO.OneAccess/
├── README.md
├── DO.OneAccess.sln
├── docs/                                         ← Documentation root
│   ├── DO.OneAccess-Specification-v1.md
│   ├── System-Architecture/
│   ├── Security/
│   ├── Access-Control/
│   ├── Frontend/
│   ├── Backend/
│   ├── Database/
│   ├── Deployment/
│   ├── Integration/
│   └── Updates/
│
├── src/
│   ├── DO.OneAccess.Domain/
│   ├── DO.OneAccess.Application/
│   ├── DO.OneAccess.Infrastructure/
│   ├── DO.OneAccess.Server/
│   └── DO.OneAccess.Client/
│
└── tests/
    ├── DO.OneAccess.UnitTests/
    └── DO.OneAccess.IntegrationTests/
```

---

## Project Details

### `DO.OneAccess.Domain`

```
DO.OneAccess.Domain/
├── Entities/
│   ├── Role.cs
│   ├── Division.cs
│   ├── Section.cs
│   ├── User.cs
│   ├── System.cs
│   ├── SystemSetting.cs
│   ├── UserSystemAccess.cs
│   ├── AdministratorScope.cs
│   ├── AdministratorSystemAccess.cs
│   ├── RefreshToken.cs
│   ├── LoginHistory.cs
│   └── AuditLog.cs
├── Enums/
│   ├── RoleType.cs
│   ├── DefaultAccess.cs
│   └── AccessOverride.cs
└── Interfaces/
    └── Repositories/
        └── IRepository.cs (and domain-specific interfaces)
```

---

### `DO.OneAccess.Application`

```
DO.OneAccess.Application/
├── Common/
│   ├── Interfaces/         ← Service contracts
│   └── Exceptions/         ← Application-level exceptions
├── DTOs/
│   ├── Access/
│   ├── Audit/
│   ├── Auth/
│   ├── Divisions/
│   ├── Sections/
│   ├── Setup/
│   ├── Systems/
│   └── Users/
├── Services/               ← Use case implementations
│   ├── IAuthService.cs
│   ├── IUserService.cs
│   ├── ISystemAccessService.cs
│   └── ...
├── Validators/             ← Input validation rules
└── Common/                 ← Shared models, exceptions, interfaces
```

---

### `DO.OneAccess.Infrastructure`

```
DO.OneAccess.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/     ← EF Core entity configurations (IEntityTypeConfiguration)
│   └── Migrations/         ← EF Core generated migrations
├── Repositories/           ← Concrete repository implementations
├── Security/
│   ├── BootstrapTokenService.cs
│   ├── JwtTokenService.cs
│   └── PasswordHasher.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

---

### `DO.OneAccess.Server`

```
DO.OneAccess.Server/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── SystemsController.cs
│   ├── DivisionsController.cs
│   ├── SectionsController.cs
│   └── ...
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── ...
├── Authorization/
│   ├── Policies/
│   └── Handlers/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── appsettings.Production.json
```

---

### `DO.OneAccess.Client`

```
DO.OneAccess.Client/
├── Pages/
│   ├── Login.razor
│   ├── Dashboard.razor
│   ├── Admin/
│   └── SystemAdmin/
├── Components/
│   ├── Layout/
│   └── Shared/
├── Services/
│   ├── AuthService.cs
│   ├── ApiClient.cs
│   └── ...
├── State/
│   └── AppState.cs
├── wwwroot/
│   ├── index.html
│   └── css/
└── Program.cs
```

---

### Test Projects

```
tests/
├── DO.OneAccess.UnitTests/
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
└── DO.OneAccess.IntegrationTests/
    ├── Api/
    └── Database/
```

---

## Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Projects | PascalCase | `DO.OneAccess.Domain` |
| Classes | PascalCase | `UserSystemAccess` |
| Interfaces | `I` prefix + PascalCase | `IUserService` |
| Methods | PascalCase | `GetUserByIdAsync` |
| Properties | PascalCase | `EmployeeNumber` |
| Private fields | `_camelCase` | `_dbContext` |
| Constants | PascalCase | `DefaultAccessPolicy` |
| Enums | PascalCase | `DefaultAccess.All` |

---

## Related Documents

| Document | Link |
|---|---|
| Architecture | [Architecture.md](Architecture.md) |
| Backend Architecture | [../Backend/Architecture.md](../Backend/Architecture.md) |
| Frontend Architecture | [../Frontend/Architecture.md](../Frontend/Architecture.md) |
