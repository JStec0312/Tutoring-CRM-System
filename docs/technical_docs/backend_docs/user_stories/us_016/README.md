# US-016 — Calculate lesson cost from rate and duration

> As a tutor, I want lesson costs to be calculated from the rate and duration so that I do not have to calculate them manually.

## Scope

The cost of a completed lesson is calculated automatically from the hourly rate configured on its `TutoringAgreement` and the lesson duration.

US-016 does not expose a dedicated endpoint. Cost calculation is triggered when a tutor marks a lesson as `Completed` through the lesson status flow implemented by US-012.

If the tutoring agreement does not have an hourly rate configured, completing the lesson does not create a charge.

Lessons marked as `Missed` are not charged.

## FILES

- Cost calculation: [HourlyRate.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/TutoringAgreements/HourlyRate.cs)
- Billing account: [BillingAccount.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/Billing/BillingAccount.cs)
- Lesson charge: [LessonCharge.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/Billing/LessonCharge.cs)
- Money value object: [Money.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/Billing/Money.cs)
- Triggering feature: [SetLessonStatus](../../../../../TutoringManagementSystem/Tutoring.Api/Features/Tutors/SetLessonStatus/)
- Unit tests: [Tutoring.UnitTest](../../../../../TutoringManagementSystem/Tutoring.UnitTest/)
- Integration tests: [SetLessonStatus](../../../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/Tutors/SetLessonStatus/)

## Trigger

US-016 is triggered by the existing US-012 endpoint:

| Method | Endpoint | Condition |
| --- | --- | --- |
| PUT | `/api/tutors/me/lessons/{lessonId}/status` | Requested status is `Completed` |

Example request:

```json
{
  "status": "Completed"
}
```

The endpoint itself belongs to US-012. US-016 extends the successful `Completed` flow with automatic lesson cost calculation and creation of a `LessonCharge`.

## Business rules

Lesson cost is calculated using:

```text
lesson cost = hourly rate × lesson duration in hours
```

Examples:

```text
100 PLN/hour × 60 minutes = 100.00 PLN
100 PLN/hour × 30 minutes = 50.00 PLN
120 PLN/hour × 90 minutes = 180.00 PLN
100 PLN/hour × 20 minutes = 33.33 PLN
```

The calculated amount is rounded to two decimal places using `MidpointRounding.AwayFromZero`.

Additional rules:

- only a `Completed` lesson creates a charge,
- a `Missed` lesson does not create a charge,
- an agreement without an hourly rate does not create a charge,
- the charge uses the currency of the agreement's hourly rate,
- one lesson can only be charged once,
- one `BillingAccount` belongs to one `TutoringAgreement`,
- subsequent completed lessons for the same agreement reuse the existing billing account.

## Implementation

When `SetLessonStatusHandler` processes a transition to `Completed`:

1. `Lesson.Complete(...)` finalizes the lesson.
2. The handler checks whether the lesson's `TutoringAgreement` has an hourly rate.
3. `HourlyRate.CalculateCost(...)` calculates the lesson cost from `Lesson.TimeSlot.Duration`.
4. The existing `BillingAccount` for the tutoring agreement is loaded.
5. If the agreement does not yet have a billing account, one is created.
6. `BillingAccount.AddLessonCharge(...)` creates a `LessonCharge`.
7. The lesson status, billing account, and lesson charge are persisted by the same `SaveChangesAsync` operation.

`HourlyRate.CalculateCost(...)` contains the cost calculation rule and returns a `Money` value object.

`BillingAccount.AddLessonCharge(...)` protects the invariant that the same lesson cannot be charged more than once.

Marking a lesson as `Missed` does not execute the billing flow.

## Tests

### Unit tests

Domain unit tests cover:

- cost calculation for different hourly rates and lesson durations,
- fractional-hour calculations,
- rounding to two decimal places,
- preservation of currency,
- rejection of zero and negative durations,
- creation of a lesson charge through `BillingAccount`,
- properties of the created charge,
- rejection of a second charge for the same lesson.

### Integration tests

`SetLessonStatusTests` verifies the integration between lesson completion and billing, including:

- completing a lesson creates a charge with the calculated amount and currency,
- a billing account is created when the first lesson is charged,
- subsequent lessons reuse the existing billing account,
- marking a lesson as `Missed` does not create a charge,
- completing a lesson without an agreement hourly rate does not create a charge.

