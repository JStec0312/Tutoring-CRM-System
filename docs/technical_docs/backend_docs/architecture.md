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

Handlers reuse [AgreementAccessQueries](../../../TutoringManagementSystem/Tutoring.Api/Features/Common/Authorization/AgreementAccessQueries.cs) to scope agreements and lessons to their participants (`ForParticipant`) or owning tutor (`ForTutor`). These query filters complement role authorization and preserve the existing missing/unowned-resource responses; they do not replace domain rules.

Registration writes the account, verification token, and outbox event together. `OutboxProcessor` publishes pending outbox messages through `RabbitMqPublisher`. RabbitMQ delivers email events to `EmailConsumer`, which delegates to `EmailEventDispatcher`; the registered handler sends mail through `SmtpMailer`. Docker Compose provides RabbitMQ and Mailpit as the local SMTP sink.

See the [backend component diagram](diagrams/backend_architecture.puml) and [request sequence](diagrams/request_flow.puml). Authentication and profile/password flows are documented per user story: [US-001](user_stories/us_001/README.md), [US-002](user_stories/us_002/README.md), [US-003](user_stories/us_003/README.md), [US-004](user_stories/us_004/README.md).

## Testing

Unit tests cover domain behavior and password policy. Integration tests run against a real SQL Server Testcontainer and cover auth, including email confirmation. The integration-test environment does not register messaging workers, so email-verification tests inspect persisted outbox data and call confirmation directly; they are not RabbitMQ-to-SMTP end-to-end tests.

[IntegrationTestBase](../../../TutoringManagementSystem/Tutoring.IntegrationTests/Infrastructure/IntegrationTestBase.cs) provides shared authentication and ProblemDetails helpers. [LessonIntegrationTestBase](../../../TutoringManagementSystem/Tutoring.IntegrationTests/Infrastructure/LessonIntegrationTestBase.cs) shares tutor/student, agreement and lesson setup across lesson tests. Keep API setup distinct from direct database setup and retain scenario-specific authorization and persistence assertions when reusing helpers.

Run verification from `TutoringManagementSystem`:

```bash
dotnet build TutoringManagementSystem.sln
dotnet test TutoringManagementSystem.sln
```
