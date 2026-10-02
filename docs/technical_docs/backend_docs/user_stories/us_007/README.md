# [US-007] Update tutoring agreement details

> As a tutor, I want to store subject and notes for each tutoring agreement so that I can keep cooperation-specific information in one place.

## Scope

An authenticated tutor can update the subject and private notes belonging to one of their tutoring agreements. The resource is a `TutoringAgreement`, identified by `TutoringAgreementId`; 

Only a non-ended agreement owned by the authenticated tutor can be updated. Hourly-rate changes are handled by the separate tutoring-agreement hourly-rate use case.

## API

| Method | Endpoint | Auth | Result |
| --- | --- | --- | --- |
| `PUT` | `/api/tutors/tutoring-agreements/{tutoringAgreementId}/details` | Authenticated user with the `Tutor` role | `204 No Content` |

Request body:

```json
{
  "subject": "Physics",
  "notes": "Bring exercises."
}
```

Both fields are nullable:

- `Subject` — replacement subject name.
- `Notes` — replacement private notes.

## Files

- [UpdateTutoringAgreementDetailsController.cs](C:/Users/stecu/OneDrive/Pulpit/inzynierka/repo/TutoringManagementSystem/Tutoring.Api/Features/Tutors/UpdateTutoringAgreementDetails/UpdateTutoringAgreementDetailsController.cs)
- [UpdateTutoringAgreementDetailsRequest.cs](C:/Users/stecu/OneDrive/Pulpit/inzynierka/repo/TutoringManagementSystem/Tutoring.Api/Features/Tutors/UpdateTutoringAgreementDetails/UpdateTutoringAgreementDetailsRequest.cs)
- [UpdateTutoringAgreementDetailsHandler.cs](C:/Users/stecu/OneDrive/Pulpit/inzynierka/repo/TutoringManagementSystem/Tutoring.Api/Features/Tutors/UpdateTutoringAgreementDetails/UpdateTutoringAgreementDetailsHandler.cs)
- [TutoringAgreement.cs](C:/Users/stecu/OneDrive/Pulpit/inzynierka/repo/TutoringManagementSystem/Tutoring.Domain/TutoringAgreements/TutoringAgreement.cs)
- [UpdateTutoringAgreementDetailsTests.cs](C:/Users/stecu/OneDrive/Pulpit/inzynierka/repo/TutoringManagementSystem/Tutoring.IntegrationTests/Features/Tutors/UpdateTutoringAgreementDetails/UpdateTutoringAgreementDetailsTests.cs)

## Implementation

- The controller reads the JWT `sub` claim as `UserAccountId`, captures request metadata (IP address, user agent, and trace ID), and dispatches `UpdateTutoringAgreementDetailsCommand`.
- The handler resolves the tutor by `UserAccountId`, then queries the requested `TutoringAgreementId` with both the tutor ownership condition and `Status != Ended`.
- A missing tutor produces `404 Not Found`. A nonexistent, ended, or unowned agreement is treated as not found and produces `404 Not Found`; the database is not changed.
- A supplied subject is created as the domain `Subject` value object. Blank subject values fail domain validation and produce `400 Bad Request` with the `Request.Invalid` problem-details code.
- Subject and notes are applied through `TutoringAgreement.UpdateTutoringAgreementDetails(...)`. The use case currently updates only `Subject` and `Notes`; `HourlyRate` is not handled here.
- Changes are persisted with `SaveChangesAsync`. Request metadata is passed through the command and structured request/update logging is emitted.
- Requests without authentication return `401 Unauthorized`; authenticated users without the `Tutor` role return `403 Forbidden`. An invalid or missing JWT subject also returns `401 Unauthorized`.

## Tests

`Tutoring.IntegrationTests`: subject-only, notes-only, and combined updates; unauthenticated and student access; nonexistent, ended, and another tutor's agreements; preservation of data when authorization/resource checks fail; invalid blank subjects; and successful persistence of the updated agreement details.
