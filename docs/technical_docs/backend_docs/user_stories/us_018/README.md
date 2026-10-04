# US-018 — View student balance

> As a tutor, I want to see a student's balance so that I know what has been paid and what is still due.

## Scope

An authenticated tutor can view the current balance for a tutoring agreement
they own. The response combines active lesson charges and recorded payments
from the agreement's billing account.

The balance is calculated when the request is processed. It is not stored as a
separate database value, and this endpoint does not create or modify billing
data. Currency is currently always `PLN`.

## API

| Method | Endpoint | Auth | Result |
| --- | --- | --- | --- |
| GET | `/api/tutors/tutoring-agreements/{tutoringAgreementId}/balance` | Authenticated `Tutor` who owns the agreement | `200 OK` with the calculated balance |

Successful response:

```json
{
  "tutoringAgreementId": "00000000-0000-0000-0000-000000000000",
  "totalCharged": 200.00,
  "totalPaid": 125.50,
  "balance": 74.50,
  "currency": "PLN"
}
```

| Field | Type | Description |
| --- | --- | --- |
| `TutoringAgreementId` | `Guid` | Requested tutoring agreement identifier |
| `TotalCharged` | `decimal` | Sum of active lesson charges |
| `TotalPaid` | `decimal` | Sum of recorded payments |
| `Balance` | `decimal` | `TotalCharged - TotalPaid` |
| `Currency` | `string` | Current currency, always `PLN` |

HTTP responses:

- `200 OK` — balance returned. An existing agreement without a
  `BillingAccount` returns zero values for `TotalCharged`, `TotalPaid`, and
  `Balance`.
- `401 Unauthorized` — the request is unauthenticated or does not contain a
  usable user identity.
- `403 Forbidden` — the authenticated user is not a tutor.
- `404 Not Found` — the tutoring agreement does not exist or belongs to
  another tutor. The handler also returns not found if the authenticated user
  account has no corresponding tutor.

## Files

- API and application flow: [GetTutoringAgreementBalance](../../../../../TutoringManagementSystem/Tutoring.Api/Features/Tutors/GetTutoringAgreementBalance/)
- Response model: [GetTutoringAgreementBalanceResponse.cs](../../../../../TutoringManagementSystem/Tutoring.Api/Features/Tutors/GetTutoringAgreementBalance/GetTutoringAgreementBalanceResponse.cs)
- Charge status: [ChargeStatus.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/Billing/ChargeStatus.cs)
- Billing aggregate: [BillingAccount.cs](../../../../../TutoringManagementSystem/Tutoring.Domain/Billing/BillingAccount.cs)
- Persistence mappings: [BillingAccountConfiguration.cs](../../../../../TutoringManagementSystem/Tutoring.Infrastructure/Persistence/Configurations/BillingAccountConfiguration.cs), [LessonChargeConfiguration.cs](../../../../../TutoringManagementSystem/Tutoring.Infrastructure/Persistence/Configurations/LessonChargeConfiguration.cs), [PaymentConfiguration.cs](../../../../../TutoringManagementSystem/Tutoring.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs)
- Integration tests: [GetTutoringAgreementBalanceTests.cs](../../../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/Tutors/GetTutoringAgreementBalance/GetTutoringAgreementBalanceTests.cs)

## Implementation

- `GetTutoringAgreementBalanceController` requires the `Tutor` role and
  passes the authenticated user account id and agreement id to the query.
- `GetTutoringAgreementBalanceHandler` resolves the tutor with an
  `AsNoTracking` query, then verifies agreement ownership using both the
  agreement id and tutor id. Missing or foreign agreements are treated
  identically as `TutoringAgreements.NotFound`.
- The handler uses an `AsNoTracking` EF Core projection over the agreement's
  `BillingAccount`. `TotalCharged` is the sum of `LessonCharge.Amount` values
  whose `ChargeStatus` is `Active`; cancelled and corrected charges are
  excluded.
- `TotalPaid` is the sum of `Payment.Amount` values from the same billing
  account. If no billing account exists, both aggregates default to zero.
- The handler calculates `Balance = TotalCharged - TotalPaid` and returns a
  `GetTutoringAgreementBalanceResponse` with the requested agreement id and
  `PLN`. No balance is persisted and the query performs no writes.

Balance semantics:

```text
TotalCharged = sum of active lesson charges
TotalPaid = sum of recorded payments
Balance = TotalCharged - TotalPaid
```

- `Balance > 0` — amount still due
- `Balance = 0` — fully settled
- `Balance < 0` — overpayment

## Diagram

![View tutoring agreement balance](diagrams/get_tutoring_agreement_balance.svg)

## Tests

`Tutoring.IntegrationTests`: zero values for an agreement without a billing
account; active-charge aggregation; multiple-payment aggregation; exclusion
of non-active charges; negative balance when payments exceed charges; missing
and foreign agreement `404` responses; unauthenticated `401` access; and
student `403` access. Successful responses also verify the agreement id and
`PLN` currency.
