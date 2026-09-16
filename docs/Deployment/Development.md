# Deployment — Development

> **Section:** Deployment  
> **Document:** Development Environment Setup  
> **Related:** [Requirements.md](Requirements.md)

---

## Prerequisites

- .NET 10 SDK installed.
- SQL Server (LocalDB or full instance) available.
- Git for source control.
- An IDE (Visual Studio 2022+ or JetBrains Rider recommended for Blazor WASM).

---

## Initial Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd DO.OneAccess
   ```

2. **Review development configuration:**
   All application configuration is stored in `appsettings` files. Development configuration, including the local database connection string and development JWT signing key (`Jwt:Key`), is maintained in:
   ```text
   src/DO.OneAccess.Server/appsettings.Development.json
   ```
   .NET User Secrets must NOT be used for application configuration.

3. **Apply database migrations:**
   ```bash
   dotnet ef database update --project src/DO.OneAccess.Infrastructure --startup-project src/DO.OneAccess.Server
   ```

4. **Run the application:**
   ```bash
   dotnet run --project src/DO.OneAccess.Server
   ```

5. **Initial System Administrator Provisioning (First Run Setup):**
   - On a clean, uninitialized deployment (`Users.Any(u => u.RoleId == 1)` is false), the server generates an ephemeral 256-bit cryptographic hex token and prints it to stdout with `[FIRST RUN SETUP]`.
   - Opening the client application automatically redirects to the `/setup` wizard.
   - Supply the console bootstrap token, administrator employee identity, and desired login credentials.
   - Upon successful submission, the initial System Administrator is created, the bootstrap gateway is permanently closed, and the operator is redirected to `/login` to sign in.
   - In production, the bootstrap token must be provided via the `ONEACCESS_BOOTSTRAP_TOKEN` environment variable; the server fails closed (disabling setup with 503) if the variable is absent. Production secrets are never auto-generated or logged.

---

## Development Configuration

All application configuration for development is stored in `appsettings` files:
- Base configuration: `src/DO.OneAccess.Server/appsettings.json`
- Development configuration (including development `Jwt:Key`): `src/DO.OneAccess.Server/appsettings.Development.json`

.NET User Secrets must NOT be used. In production and staging deployments, configuration is stored in environment-specific appsettings files (`appsettings.Production.json` / `appsettings.Staging.json`).

---

## Notes

- HTTP redirection to HTTPS may be disabled in development for convenience. Confirm this is not applied to staging/production.
- Developer HTTPS certificates: `dotnet dev-certs https --trust`.

---

## Related Documents

| Document | Link |
|---|---|
| Requirements | [Requirements.md](Requirements.md) |
| Staging | [Staging.md](Staging.md) |
| Production | [Production.md](Production.md) |
| Migration Strategy | [../Database/Migration-Strategy.md](../Database/Migration-Strategy.md) |
