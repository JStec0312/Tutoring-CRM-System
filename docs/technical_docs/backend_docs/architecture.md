# Backend Architecture

The backend uses Vertical Slice Architecture with MediatR and EF Core. Use-case logic lives in feature slices inside `Tutoring.Api`; there is no separate Application project, repository abstraction, or Unit of Work abstraction.

Authentication behavior is documented separately in [Authentication](authentication.md).

## Components and dependencies

| Project | Responsibility |
| --- | --- |
| `Tutoring.Api` | ASP.NET Core startup, controllers and HTTP contracts, MediatR commands and handlers, authentication/authorization setup, logging, OpenAPI, and global exception handling. |
| `Tutoring.Domain` | Entities, value objects, strongly typed identifiers, domain behavior, and domain exceptions. It has no project references. |
| `Tutoring.Infrastructure` | `TutoringDbContext`, EF Core mappings and migrations, SQL Server registration, password hashing and policy, and JWT/refresh-token services. |
| `Tutoring.UnitTest` | Isolated tests for domain types and the password policy validator. |
| `Tutoring.IntegrationTests` | End-to-end HTTP tests using `WebApplicationFactory`, SQL Server Testcontainers, EF migrations, and Respawn database resets. |

Production dependency direction is:

```text
Tutoring.Api -> Tutoring.Infrastructure -> Tutoring.Domain
```

The Domain project must not depend on API, MediatR, EF Core, or Infrastructure. API code also consumes Domain types through Infrastructure's transitive project reference. The exact project and runtime dependencies are shown in the [backend component diagram](diagrams/backend_architecture.puml).

## Vertical slices and request flow

API features are grouped by use case under `Tutoring.Api/Features/{Area}/{UseCase}/`. A slice contains only the artifacts it needs, typically a controller, HTTP request/response types, a MediatR command, a handler, and any handler result or feature exception.

A request normally follows this path:

1. A controller handles HTTP concerns, creates a command, and sends it through MediatR.
2. The handler implements the use case, queries or updates `TutoringDbContext`, and invokes domain constructors or behavior where required.
3. EF Core applies mappings from Infrastructure and persists to SQL Server.
4. The controller converts the handler result into the HTTP response.

See the [request sequence diagram](diagrams/request_flow.puml).

## Placement rules

| Concern | Location |
| --- | --- |
| Routes, cookies, claims, status codes, request metadata, HTTP contracts | Controller and adjacent types in `Tutoring.Api/Features` |
| Use-case orchestration and database-dependent checks | Feature handler in `Tutoring.Api/Features` |
| Invariants and behavior intrinsic to the business model | `Tutoring.Domain` |
| EF mappings, migrations, database registration | `Tutoring.Infrastructure/Persistence` |
| Password, JWT, and refresh-token implementations | `Tutoring.Infrastructure/Authentication` |

Handlers use `TutoringDbContext` directly. Async EF calls receive the request `CancellationToken`; read-only entity queries use `AsNoTracking()` where tracking is unnecessary. EF-specific configuration stays outside domain types.

## Exception handling

`GlobalExceptionHandler` converts known exceptions to RFC 7807 `ProblemDetails` and adds the application error code as the `code` extension.

| Exception | HTTP status |
| --- | --- |
| `NotFoundException` | 404 Not Found |
| `ConflictException` | 409 Conflict |
| `ForbiddenException` | 403 Forbidden |
| `AuthException` | 401 Unauthorized |
| `DomainException` | 422 Unprocessable Entity |
| `UseCaseException` | 400 Bad Request |
| Any other exception | 500 Internal Server Error |

Feature-specific failures belong near the feature; shared API exception bases live under `Features/Common/Exceptions`. Domain invariant failures belong in Domain.

## Testing

Unit tests cover domain construction, equality, invariants, behavior, and password-policy validation without HTTP or a database. Integration tests exercise real API endpoints and persistence against a disposable SQL Server container. Auth integration tests cover registration, login, refresh rotation and reuse detection, sign-out, and sign-out-all.

Run backend verification from `TutoringManagementSystem`:

```bash
dotnet build TutoringManagementSystem.sln
dotnet test TutoringManagementSystem.sln
```
