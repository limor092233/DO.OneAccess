# Backend — Validation

> **Section:** Backend  
> **Document:** Validation  
> **Related:** [Application-Layer.md](Application-Layer.md) | [API-Design.md](API-Design.md)

---

## Validation Philosophy

Validation in DO.OneAccess is applied at the Application layer before any business logic or data access is performed. The API surface provides validation error responses to the client.

---

## Validation Strategy

| Layer | Responsibility |
|---|---|
| Client | UX-level validation (form field hints, basic required checks) — **not a security control** |
| API Controller | Minimal; rely on model binding; delegate to Application layer |
| Application Layer | Authoritative validation via validator classes |
| Domain | Domain invariants enforced in entities; not duplicated in validators |

---

## Validation Implementation

Validation is implemented using dedicated validator classes and service-level validation in the Application layer (`DO.OneAccess.Application`):

- Validator classes (e.g., `FirstRunSetupDtoValidator` in `Application/Validators/`) perform fail-fast structural and business format validations.
- Application services (`UserService`, `DivisionService`, `SectionService`, `SystemService`, `SystemAccessService`, `AdminScopeService`) enforce domain business invariants, scope rules, and state validations.
- Failed validations throw `ValidationException` containing structured field errors (`IDictionary<string, string[]>`).

---

## Validation Rules (General)

All input DTOs must validate:

| Rule | Example |
|---|---|
| Required fields | `EmployeeNumber`, `FirstName`, `LastName`, `Email`, `Username` must not be empty |
| Optional fields | `Position` (max 100 chars, optional), `MiddleName` (max 100 chars, optional) |
| Length constraints | Name between 1–100 characters; Username max 50; EmployeeNumber max 50 |
| Format constraints | Email must be valid format |
| Role constraints | `RoleId` must reference an existing active role (`smallint`) |
| SectionId constraints | For `USER`, `SectionId` is required; for `ADMINISTRATOR`, `SectionId` is optional; for `SYSTEM_ADMINISTRATOR`, `SectionId` must be null |
| Identifier constraints | ID parameters must match the entity's tiered identifier type (GUID for Users, Systems; integer/bigint for Roles, Divisions, Sections, Scopes, Access records) |

---

## Uniqueness Checks

Uniqueness (e.g., `EmployeeNumber`, `Username`, `SystemCode`) is enforced:

1. At the database level via unique constraints / unique indexes.
2. In the Application services via pre-check queries before insert/update (to return meaningful validation or conflict errors before hitting DB constraint violations).

On unique constraint violation or duplicate state conflicts, services throw `ConflictException` (mapped to HTTP 409).

---

## Validation Error Response Format

> **Decision (Approved):** Validation errors return HTTP 400 with ASP.NET Core `ProblemDetails` (RFC 7807), including an `errors` extension for field-level validation messages. Do not use a custom error envelope or `Result<T>` wrapper.

On validation failure, the API returns `400 Bad Request` with a `ProblemDetails` body:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "employeeNumber": ["Employee number is required."],
    "email": ["Email must be a valid email address."]
  }
}
```

This is consistent with ASP.NET Core's built-in `ValidationProblemDetails` format. `ValidationException` failures are mapped to this format (e.g., via `ProblemDetails` middleware/filters that convert `ValidationException` to `ValidationProblemDetails`).


---

## Input Sanitization

- Do not trust client-provided data.
- Validate and reject invalid input before processing.
- Do not rely on the client to sanitize inputs.
- String inputs should be trimmed before storage (configurable per field).

---

## Related Documents

| Document | Link |
|---|---|
| Application Layer | [Application-Layer.md](Application-Layer.md) |
| API Design | [API-Design.md](API-Design.md) |
| Security Guidelines | [../Security/Security-Guidelines.md](../Security/Security-Guidelines.md) |
