# Backend Architecture

The backend uses Vertical Slice Architecture, MediatR, and EF Core. Use cases live in `Tutoring.Api/Features/{Area}/{UseCase}`. There is no Application project, Repository abstraction, or Unit of Work abstraction.

## Projects

| Project | Responsibility |
| --- | --- |
| `Tutoring.Api` | ASP.NET Core host, HTTP contracts/controllers, feature handlers, MediatR, authentication, and exception handling. |
| `Tutoring.Infrastructure` | `TutoringDbContext`, EF mappings/migrations, SQL Server, authentication services, outbox, RabbitMQ, and SMTP implementations. |
| `Tutoring.Domain` | Entities, value objects, typed IDs, invariants, and domain exceptions. |
| `Tutoring.UnitTest` | Isolated domain and password-policy tests. |
| `Tutoring.IntegrationTests` | HTTP integration tests with SQL Server Testcontainers, migrations, and Respawn resets. |

Production references flow as `Tutoring.Api -> Tutoring.Infrastructure -> Tutoring.Domain`. The Domain project has no dependency on API or Infrastructure.

## Request and messaging flow

Controllers keep HTTP concerns at the edge and send commands through MediatR. Handlers orchestrate use cases and access `TutoringDbContext` directly; read-only queries use `AsNoTracking()` where appropriate. EF mappings and migrations remain in Infrastructure.

Registration writes the account, verification token, and outbox event together. `OutboxProcessor` publishes pending outbox messages through `RabbitMqPublisher`. RabbitMQ delivers email events to `EmailConsumer`, which delegates to `EmailEventDispatcher`; the registered handler sends mail through `SmtpMailer`. Docker Compose provides RabbitMQ and Mailpit as the local SMTP sink.

See the [backend component diagram](diagrams/backend_architecture.puml), [request sequence](diagrams/request_flow.puml), and [authentication flow](authentication.md).

## Testing

Unit tests cover domain behavior and password policy. Integration tests run against a real SQL Server Testcontainer and cover auth, including email confirmation. The integration-test environment does not register messaging workers, so email-verification tests inspect persisted outbox data and call confirmation directly; they are not RabbitMQ-to-SMTP end-to-end tests.

Run verification from `TutoringManagementSystem`:

```bash
dotnet build TutoringManagementSystem.sln
dotnet test TutoringManagementSystem.sln
```
