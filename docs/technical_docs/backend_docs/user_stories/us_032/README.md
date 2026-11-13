# US-032

> As a tutor, I want to see statistics for a specific student, including lessons, attendance, and payments, so that I can assess cooperation individually.

Implemented.

`GET /api/tutors/me/students/{studentId}/statistics?from=2026-01-01&to=2026-01-31`

Only authenticated tutors can access the endpoint. It includes all agreements between the authenticated tutor and the requested student, including ended agreements.

`from` and `to` are optional UTC calendar dates in `YYYY-MM-DD` format. Each supplied boundary is inclusive. With both omitted, the endpoint includes all lesson dates. The lesson's start date determines whether the lesson and its charges are included.

| Response section | Fields and rules |
| --- | --- |
| `lessons` | `scheduledCount`, `completedCount`, `cancelledCount`, `missedCount`, and `completedMinutes` based on the duration of completed lessons, with second precision. |
| `attendance` | `attendanceRatePercent` is the percentage of completed lessons among completed and missed lessons, or `null` when both counts are zero. `cancelledByStudentCount` and `cancelledByTutorCount` show who cancelled. |
| `payments` | An array sorted by `currency`. Each entry contains `paidAmount`, `unpaidAmount`, and `unpaidChargeCount`. The totals use active charges and their current payment status. A payment covering several lessons contributes only the charge amounts for lessons within the requested range. |

A valid agreement with no matching lessons returns zero counts, undefined attendance, and an empty payments array.

- `400` for malformed dates or `from > to` (`Request.Invalid` for a reversed range).
- `401` for missing or invalid authentication and `403` for a non-tutor role.
- `404` with `TutoringAgreements.NotFound` if the tutor has no agreement with the requested student, including when the student does not exist. A missing tutor profile returns `Tutors.NotFound`.

[Implementation](../../../../../TutoringManagementSystem/Tutoring.Api/Features/Tutors/GetStudentStatistics) · [Integration tests](../../../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/Tutors/GetStudentStatistics/GetStudentStatisticsTests.cs) · [PlantUML sequence diagram](student-statistics.puml)
