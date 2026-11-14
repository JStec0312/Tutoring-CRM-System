# Tutor workspace

The workspace implements the revised Tutor flows on web and mobile. It uses the same routes and API data across screen sizes. [Tutor wireframes](https://www.figma.com/design/QVyHQq9rKy4uUENkMgiVeL/Tutoring-Wireframes?node-id=34-26)

## Screens

| Design | Route | Behavior | Backend |
| --- | --- | --- | --- |
| T01 Today | `/tutor` | Shows scheduled lessons overlapping the local day that have yet to end, past scheduled lessons awaiting an outcome, and unpaid totals per currency. The shortcuts open scheduling, notifications, payments, student creation, and statistics. | [Calendar](../../technical_docs/backend_docs/user_stories/us_013/README.md), [overdue payments](../../technical_docs/backend_docs/user_stories/us_019/README.md) |
| T02 Students | `/tutor/students` | Groups agreements by student and searches by display name or email. **Active** matches students with an active agreement, **Inactive** matches students with only non-active agreements, and **All** shows both. A card opens the student profile and its agreement links. The screen also shows pending invitations. | [Assigned students](../../technical_docs/backend_docs/user_stories/us_005/README.md), [invitations](../../technical_docs/backend_docs/user_stories/us_006/README.md) |
| T03 Add student manually | `/tutor/students/add` | Collects a student name, agreement title, subject, and optional PLN hourly rate. The form accepts either a comma or a period as the decimal separator. Successful creation opens the new agreement. The email variant at `?mode=invite` replaces the name with the registered student's email and returns to the list after submission. | [Manual creation and email invitations](../../technical_docs/backend_docs/user_stories/us_006/README.md) |
| T04 Agreement | `/tutor/agreements/[agreementId]` | Shows the agreement status and rate, next lesson, recent lesson, unpaid charges, and material count. History and statistics use lessons for the selected agreement. The settings panel saves subject and notes, hourly rate, and activation status through separate actions. | [Agreement overview and details](../../technical_docs/backend_docs/user_stories/us_007/README.md), [status](../../technical_docs/backend_docs/user_stories/us_008/README.md), [rate](../../technical_docs/backend_docs/user_stories/us_015/README.md), [lesson history](../../technical_docs/backend_docs/user_stories/us_014/README.md) |
| T05 Calendar | `/tutor/calendar` | Opens in week mode and supports day and month views, previous and next periods, and a jump to today. The calendar follows the device time zone and starts weeks on Monday. Selecting a date in month mode opens its day view. Selecting a lesson opens its details. | [Calendar and lesson details](../../technical_docs/backend_docs/user_stories/us_013/README.md) |

## Linked routes

| Route | Behavior |
| --- | --- |
| `/tutor/students/[studentId]` | Links to agreements, student statistics and the form for adding an agreement to this student. |
| `/tutor/schedule`, `/tutor/lessons/[lessonId]/edit` | Uses the local date, start time, duration and optional planned topic to schedule or reschedule a lesson. The form previews the estimated cost and keeps the values when a save fails. |
| `/tutor/lessons/[lessonId]`, `/tutor/lessons/[lessonId]/cancel` | Shows lesson details, notes and attached materials, and supports rescheduling, cancellation with a reason, and marking ended lessons completed or missed. Completed, missed and cancelled use the current lesson status. |
| `/tutor/payments`, `/tutor/payments/[chargeId]` | Filters paid and unpaid charges by currency, agreement or lesson and records a payment for an existing charge. Totals stay separate by currency. |
| `/tutor/materials`, `/tutor/materials/upload`, `/tutor/materials/[materialId]/attach`, `/tutor/materials/[materialId]/delete` | Lists, downloads, uploads, attaches and removes materials within a selected agreement. Upload accepts PDF, JPG and PNG files up to 20 MB. |
| `/tutor/statistics`, `/tutor/students/[studentId]/statistics` | Shows weekly, monthly and yearly statistics, with attendance, agreement receivables and lesson history. Periods follow the UTC reporting API. |
| `/tutor/notifications` | Shows lesson reminders for the next day, recent cancelled lessons and past scheduled lessons awaiting an outcome. The app derives these entries from calendar data. |
| `/tutor/profile`, `/tutor/more` | Edits the authenticated user's profile and password. More links to materials, statistics, notifications and profile on narrow screens. |

## Code structure

The [authenticated app layout](../../../mobile/src/app/%28app%29/_layout.tsx) guards the entire `(tutor)` route group. The [tutor layout](../../../mobile/src/app/%28app%29/%28tutor%29/_layout.tsx) wraps the route slot with the workspace header and navigation. Narrow layouts move navigation from the sidebar to the bottom and stack the main content.

Each use case under [`features/tutor`](../../../mobile/src/features/tutor/) owns its screen in `components/` and HTTP calls in `api/`. Use-case-specific logic such as [student form validation](../../../mobile/src/features/tutor/add-student/models/validate-student-form.ts) lives in `models/`. The [workspace module](../../../mobile/src/features/tutor/workspace/) contains tutor-specific layout, route helpers, formatting, and query state. The [shared UI directory](../../../mobile/src/shared/components/ui/) contains the reusable navigation and controls.

Screens use the [authenticated API client](../../../mobile/src/shared/api/api-client.ts). [`useTutorQuery`](../../../mobile/src/features/tutor/workspace/models/use-tutor-query.ts) loads on focus, aborts obsolete GET requests, and exposes loading, error, and retry states. The calendar converts local day boundaries to UTC in requests while displaying lesson times in the device time zone.

## Verification

The [Today API tests](../../../mobile/tests/features/tutor/dashboard-api.test.ts) cover local-day query bounds, lessons awaiting an outcome, currency totals, and load errors. The [calendar integration tests](../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/Calendar/GetCalendarLessons/GetCalendarLessonsTests.cs) exercise the tutor calendar filters, agreement overview, and lesson details against SQL Server.
