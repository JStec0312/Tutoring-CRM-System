# Development Data Bootstrap

The application includes a development data bootstrapper that can be triggered with:

```http
POST /api/dev/bootstrap
```

The bootstrap creates a deterministic set of development data. Entity IDs are hardcoded so the same IDs can be reused in Scalar, Postman, scripts, and manual database queries.

## Test Accounts

| Type | ID | Email | Password |
|---|---|---|---|
| Tutor account | `00000000-0000-0000-0000-000000000001` | `tutor.dev@test.pl` | `Password123!` |
| Student Anna account | `00000000-0000-0000-0000-000000000002` | `student1.dev@test.pl` | `Password123!` |
| Student Piotr account | `00000000-0000-0000-0000-000000000003` | `student2.dev@test.pl` | `Password123!` |

## Domain Data
`
| Entity | ID | Details |`
|---|---|---|
| Tutor | `00000000-0000-0000-0000-000000000101` | Development Tutor |
| Student | `00000000-0000-0000-0000-000000000201` | Anna Nowak |
| Student | `00000000-0000-0000-0000-000000000202` | Piotr Kowalski |
| Student | `00000000-0000-0000-0000-000000000203` | Manual Student, no user account |
| Tutoring Agreement | `00000000-0000-0000-0000-000000000301` | Mathematics, 100 PLN/h, Active |
| Tutoring Agreement | `00000000-0000-0000-0000-000000000302` | Physics, 120 PLN/h, Active |
| Tutoring Agreement | `00000000-0000-0000-0000-000000000303` | Programming, no hourly rate, Suspended |
| Lesson | `00000000-0000-0000-0000-000000000401` | Completed |
| Lesson | `00000000-0000-0000-0000-000000000402` | Missed |
| Lesson | `00000000-0000-0000-0000-000000000403` | Cancelled |
| Lesson | `00000000-0000-0000-0000-000000000404` | Scheduled Mathematics lesson |
| Lesson | `00000000-0000-0000-0000-000000000405` | Scheduled Physics lesson |
| Billing Account | `00000000-0000-0000-0000-000000000501` | Mathematics agreement billing account |
| Lesson Charge | `00000000-0000-0000-0000-000000000502` | Charge for the completed lesson |
