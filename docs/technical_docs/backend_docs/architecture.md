# Tutoring CRM System Architecture

## 1. Purpose

This document describes the technical architecture and code organization of the Tutoring CRM System.

It is intended for both developers and AI coding agents working with the repository.

The main goal is to provide a single source of truth for:

* project responsibilities,
* dependency rules,
* feature organization,
* domain modeling,
* persistence,
* testing,
* exception handling,
* implementation conventions.

When adding new functionality, existing patterns described in this document should be followed unless an architectural decision explicitly changes them.

---

## 2. Architectural Style

The backend combines:

* Vertical Slice Architecture,
* CQRS using MediatR,
* Domain-Driven Design concepts,
* Clean Architecture dependency principles,
* Entity Framework Core for persistence.

The solution intentionally does **not** contain a separate `Application` project.

Application use-case logic is organized directly inside feature slices in `Tutoring.Api`.

This keeps functionality grouped by use case instead of splitting a single feature across multiple technical layers.

---

## 3. Solution Structure

```text
TutoringManagementSystem/
├── Tutoring.Api/
├── Tutoring.Domain/
├── Tutoring.Infrastructure/
├── Tutoring.UnitTests/
├── Tutoring.IntegrationTests/
└── TutoringManagementSystem.sln
```

### Tutoring.Api

Contains the application entry point and use cases.

Responsibilities include:

* ASP.NET Core configuration,
* controllers,
* HTTP request models,
* HTTP response models,
* MediatR commands and queries,
* MediatR handlers,
* use-case exceptions,
* authentication and authorization configuration,
* global exception handling,
* OpenAPI configuration.

Features are organized using Vertical Slice Architecture.

Example:

```text
Tutoring.Api/
└── Features/
    └── Auth/
        └── RegisterStudent/
            ├── RegisterStudentController.cs
            ├── RegisterStudentRequest.cs
            ├── RegisterStudentCommand.cs
            ├── RegisterStudentHandler.cs
            └── RegisterStudentResponse.cs
```

Each use case should contain the files required to implement that use case.

Do not create global folders such as:

```text
Controllers/
Commands/
Queries/
Handlers/
Requests/
Responses/
```

Use-case files should remain grouped together.

---

### Tutoring.Domain

Contains the business model.

Responsibilities include:

* entities,
* aggregate roots,
* value objects,
* strongly typed identifiers,
* domain invariants,
* domain behavior,
* domain exceptions,
* business rules.

Example:

```text
Tutoring.Domain/
├── Common/
│   ├── Entity.cs
│   ├── DomainId.cs
│   ├── Guard.cs
│   └── Exceptions/
│
├── Identity/
│   ├── UserAccount.cs
│   ├── UserAccountId.cs
│   ├── PersonalProfile.cs
│   ├── PasswordHash.cs
│   └── UserRole.cs
│
├── Students/
│   ├── Student.cs
│   ├── StudentId.cs
│   └── StudentStatus.cs
│
└── Tutors/
    ├── Tutor.cs
    ├── TutorId.cs
    └── TutorStatus.cs
```

The Domain project must not depend on:

* ASP.NET Core,
* Entity Framework Core,
* MediatR,
* Infrastructure,
* HTTP concepts.

Domain objects should protect their own invariants.

Invalid domain state should be rejected at the point where the domain object is created or modified.

---

### Tutoring.Infrastructure

Contains technical persistence implementation.

Responsibilities include:

* `TutoringDbContext`,
* Entity Framework Core configurations,
* database mappings,
* migrations,
* database-specific configuration.

Example:

```text
Tutoring.Infrastructure/
└── Persistence/
    ├── TutoringDbContext.cs
    ├── Configurations/
    │   ├── UserAccountConfiguration.cs
    │   ├── StudentConfiguration.cs
    │   └── TutorConfiguration.cs
    └── Migrations/
```

EF Core configuration should remain outside domain entities.

Domain classes should not contain EF-specific attributes unless there is a strong reason to introduce them.

---

## 4. Project Dependencies

The expected dependency direction is:

```text
Tutoring.Api
    |
    v
Tutoring.Infrastructure
    |
    v
Tutoring.Domain
```

`Tutoring.Api` may use both Infrastructure and Domain types through its dependency on Infrastructure.

`Tutoring.Infrastructure` depends on `Tutoring.Domain`.

`Tutoring.Domain` must remain independent.

The dependency direction must never be reversed.

For example:

```text
Domain -> Infrastructure
```

is forbidden.

---

## 5. Vertical Slice Organization

Features should be grouped by business area and use case.

Example:

```text
Features/
├── Auth/
│   ├── RegisterStudent/
│   ├── RegisterTutor/
│   └── Login/
│
├── Students/
│   ├── GetStudentById/
│   ├── UpdateStudent/
│   └── DeleteStudent/
│
└── Lessons/
    ├── CreateLesson/
    ├── CancelLesson/
    └── GetLessonById/
```

Each directory represents one application use case.

A command slice may look like:

```text
RegisterStudent/
├── RegisterStudentController.cs
├── RegisterStudentRequest.cs
├── RegisterStudentCommand.cs
├── RegisterStudentHandler.cs
└── RegisterStudentResponse.cs
```

A query slice may look like:

```text
GetStudentById/
├── GetStudentByIdController.cs
├── GetStudentByIdQuery.cs
├── GetStudentByIdHandler.cs
└── GetStudentByIdResponse.cs
```

Do not introduce additional abstraction layers without a concrete reason.

---

## 6. Request Flow

Typical request processing:

```text
HTTP Client
    |
    v
Controller
    |
    v
MediatR
    |
    v
Command / Query Handler
    |
    +----> Domain objects
    |
    v
TutoringDbContext
    |
    v
SQL Server
```

### Controller

The controller is responsible for HTTP concerns.

Typical responsibilities:

* receiving request data,
* mapping the HTTP request to a command or query,
* sending it through MediatR,
* returning the HTTP response.

Controllers should remain thin.

Business logic should not be implemented directly inside controllers.

---

### Command / Query

Commands represent operations that change application state.

Examples:

```text
RegisterStudentCommand
CreateLessonCommand
CancelLessonCommand
```

Queries represent read operations.

Examples:

```text
GetStudentByIdQuery
GetLessonByIdQuery
```

Commands and queries are sent through MediatR.

---

### Handler

The handler implements the use case.

It may:

* query the database,
* check use-case-specific conditions,
* create domain value objects,
* load domain entities,
* execute domain behavior,
* persist changes,
* return a response.

Handlers use `TutoringDbContext` directly.

Do not introduce repository or Unit of Work abstractions around Entity Framework Core unless a future architectural requirement clearly justifies them.

---

## 7. Domain Rules vs Use-Case Rules

A distinction must be maintained between domain validation and use-case validation.

### Domain rules

Rules that determine whether an object itself can exist in a valid state belong in the Domain project.

Examples:

```text
EmailAddress must contain a valid email address.
TimeSlot end time must be later than start time.
Money cannot contain an unsupported currency.
```

These rules should normally be enforced inside constructors or domain methods.

---

### Use-case rules

Rules requiring application state, database access or use-case context belong in the feature handler.

Examples:

```text
Email must be unique.
Username must be unique.
Student must exist before retrieving their profile.
A tutor cannot create two conflicting lessons.
```

These rules usually require access to `TutoringDbContext`.

---

## 8. Value Objects

Value objects represent domain concepts that have validation or behavior beyond a primitive value.

Examples include:

```text
EmailAddress
PhoneNumber
PasswordHash
Money
TimeSlot
HourlyRate
Subject
```

Prefer:

```csharp
EmailAddress email
```

over:

```csharp
string email
```

inside the domain when the value has domain meaning or invariants.

Value objects should validate themselves during creation.

They should not contain HTTP or persistence-specific behavior.

---

## 9. Strongly Typed IDs

Domain entities use strongly typed identifiers.

Example:

```csharp
StudentId
UserAccountId
TutorId
LessonId
```

instead of passing raw `Guid` values throughout the domain.

This prevents accidental mixing of unrelated identifiers.

Example:

```csharp
StudentId studentId;
TutorId tutorId;
```

cannot accidentally be used interchangeably.

The common identifier abstraction is located in:

```text
Tutoring.Domain/Common/DomainId.cs
```

EF Core conversion and generation logic belongs in Infrastructure.

---

## 10. Persistence

The system uses Entity Framework Core with SQL Server.

A single application `DbContext` is used:

```text
TutoringDbContext
```

Handlers may use `TutoringDbContext` directly.

### Queries

Queries should normally use:

```csharp
AsNoTracking()
```

when entities are only being read.

Prefer projecting directly into response models where appropriate.

Example:

```csharp
var response = await dbContext.Students
    .AsNoTracking()
    .Where(student => student.Id == request.Id)
    .Select(student => new GetStudentByIdResponse(...))
    .SingleOrDefaultAsync(cancellationToken);
```

This avoids loading unnecessary domain objects for simple read operations.

---

### Commands

Commands should load or create domain entities, execute domain behavior and save changes.

Example:

```csharp
var student = new Student(userAccount.Id);

dbContext.Students.Add(student);

await dbContext.SaveChangesAsync(cancellationToken);
```

All asynchronous EF Core operations should receive the provided `CancellationToken`.

---

## 11. Entity Framework Configuration

Database mappings belong in:

```text
Tutoring.Infrastructure/Persistence/Configurations/
```

Each major entity should normally have its own configuration.

Example:

```text
StudentConfiguration.cs
UserAccountConfiguration.cs
LessonConfiguration.cs
```

Use:

```csharp
IEntityTypeConfiguration<TEntity>
```

and register configurations through:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(
    typeof(TutoringDbContext).Assembly);
```

Configuration files may contain:

* table names,
* column names,
* property conversions,
* maximum lengths,
* indexes,
* relationships,
* cascade behavior,
* owned value-object mappings.

---

## 12. Exceptions

The system separates domain failures from application/use-case failures.

### DomainException

Represents violations of domain invariants.

Examples:

```text
InvalidEmailAddressException
EmptyFieldException
```

Domain exceptions belong in `Tutoring.Domain`.

---

### UseCaseException

Represents failures of a particular application operation.

Examples:

```text
StudentNotFoundException
EmailAlreadyTakenException
UsernameAlreadyTakenException
```

Use-case exceptions belong near their corresponding API feature or feature area.

Common application exception types may exist under:

```text
Tutoring.Api/Features/Common/Exceptions/
```

Current hierarchy includes:

```text
UseCaseException
├── NotFoundException
└── ConflictException
```

---

## 13. HTTP Exception Mapping

Exceptions are mapped centrally by `GlobalExceptionHandler`.

Current mapping:

```text
NotFoundException  -> 404 Not Found
ConflictException  -> 409 Conflict
DomainException    -> 422 Unprocessable Entity
UseCaseException   -> 400 Bad Request
Other exception    -> 500 Internal Server Error
```

The API returns ASP.NET Core `ProblemDetails`.

Example error structure:

```json
{
  "status": 409,
  "title": "Conflict",
  "detail": "Email is already taken.",
  "code": "Auth.EmailAlreadyTaken"
}
```

Controllers should not normally contain local `try/catch` blocks for these exceptions.

---

## 14. Configuration

Application configuration that is represented directly by application-owned classes should remain centralized and explicit.

Configuration values should not be duplicated throughout handlers.

For example, password policy rules should be represented by a dedicated configuration/policy class and consumed by authentication use cases.

Feature handlers should not contain unrelated hard-coded configuration values.

---

## 15. Unit Tests

Unit tests verify domain behavior in isolation.

They belong in:

```text
Tutoring.UnitTests/
```

The directory structure should mirror the Domain project where practical.

Example:

```text
Tutoring.Domain/
└── Identity/
    ├── UserAccount.cs
    └── PasswordHash.cs

Tutoring.UnitTests/
└── Identity/
    ├── UserAccountTests.cs
    └── PasswordHashTests.cs
```

Unit tests should cover:

* valid construction,
* invalid construction,
* domain invariants,
* entity behavior,
* value-object equality,
* state transitions.

Unit tests should not require:

* a database,
* HTTP,
* ASP.NET Core,
* Testcontainers.

---

## 16. Integration Tests

Integration tests verify complete backend use cases.

They belong in:

```text
Tutoring.IntegrationTests/
```

The structure should mirror API features.

Example:

```text
Tutoring.IntegrationTests/
└── Features/
    └── Auth/
        └── RegisterStudent/
            └── RegisterStudentTests.cs
```

Integration tests use:

* `WebApplicationFactory`,
* real ASP.NET Core HTTP endpoints,
* SQL Server Testcontainers,
* Entity Framework Core migrations,
* Respawn for database cleanup.

A typical integration test verifies:

```text
HTTP request
    |
    v
API
    |
    v
Handler
    |
    v
EF Core
    |
    v
SQL Server
```

Integration tests should test observable application behavior rather than internal implementation details.

Important scenarios should normally include:

* successful operation,
* invalid input,
* missing resources,
* conflicts,
* persistence of expected state.

---

## 17. Naming Conventions

Use cases follow the naming convention:

```text
{UseCase}Controller
{UseCase}Request
{UseCase}Command
{UseCase}Query
{UseCase}Handler
{UseCase}Response
```

Examples:

```text
RegisterStudentController
RegisterStudentRequest
RegisterStudentCommand
RegisterStudentHandler
RegisterStudentResponse
```

and:

```text
GetStudentByIdController
GetStudentByIdQuery
GetStudentByIdHandler
GetStudentByIdResponse
```

Classes should generally be declared `sealed` unless inheritance is required.

---

## 18. General Implementation Rules

When implementing new functionality:

1. Identify the business area.

2. Identify the use case.

3. Create the feature under:

```text
Tutoring.Api/Features/{Area}/{UseCase}/
```

4. Keep HTTP concerns in the controller and request/response models.

5. Keep use-case orchestration in the handler.

6. Keep domain invariants and business behavior in Domain objects.

7. Keep EF Core mappings in Infrastructure.

8. Use strongly typed domain IDs.

9. Use domain value objects instead of primitive values where appropriate.

10. Use `TutoringDbContext` directly instead of introducing repositories.

11. Pass `CancellationToken` to asynchronous database operations.

12. Use `AsNoTracking()` for read-only queries.

13. Use the existing exception hierarchy.

14. Add unit tests for new domain behavior.

15. Add integration tests for new API functionality.

16. Do not leave incomplete implementations, dead code or commented-out alternative implementations on the main branch.

17. Do not introduce a new architectural pattern when an existing project pattern already solves the problem.

---

## 19. Rules for AI Coding Agents

AI agents modifying this repository must treat this document and the current established code structure as architectural constraints.

Agents should:

* inspect existing nearby features before creating new code,
* follow existing naming and folder conventions,
* group files by use case,
* reuse existing domain abstractions,
* preserve dependency direction,
* avoid unnecessary abstractions,
* add or update tests together with production code,
* remove obsolete code instead of leaving competing implementations.

Agents must not introduce:

```text
Tutoring.Application
Repository<T>
IUnitOfWork
Controllers/
Handlers/
Commands/
Queries/
```

as new architectural layers or global technical folders unless explicitly requested.

When multiple implementation approaches are possible, prefer the approach already established in the repository.

A single consistent implementation pattern is more important than introducing a theoretically cleaner but incompatible local abstraction.

---

## 20. Verification

Before considering backend work complete, run:

```bash
dotnet restore
dotnet build
dotnet test
```

The complete solution must build successfully and all tests must pass.

New functionality should not be considered complete if:

* the implementation is unfinished,
* compilation warnings indicate a real issue,
* existing tests fail,
* required tests were not added,
* obsolete competing code remains in the repository.

---

## 21. Architectural Summary

The expected architecture is:

```text
HTTP
 |
 v
Tutoring.Api
 ├── Controllers
 ├── Requests / Responses
 ├── Commands / Queries
 └── Handlers
          |
          +------> Tutoring.Domain
          |
          v
 TutoringDbContext
          |
          v
Tutoring.Infrastructure
          |
          v
      SQL Server
```

The core principles are:

```text
Organize by feature.
Keep the domain independent.
Keep controllers thin.
Keep handlers focused on use-case orchestration.
Keep business invariants in the domain.
Use EF Core directly.
Test domain behavior with unit tests.
Test complete use cases with integration tests.
Prefer consistency over unnecessary abstraction.
```
