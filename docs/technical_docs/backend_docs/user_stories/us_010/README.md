# US-010

> As a tutor, I want to edit a scheduled lesson so that I can correct its time or details.

The tutor can reschedule an owned scheduled lesson and update its planned topic.

## API

`PUT /api/tutors/me/lessons/{lessonId}/schedule` requires the Tutor role and accepts `startsAt`, `durationMinutes` and optional `topic`. A successful update returns `204 No Content`.

The start time must be in the future and the duration must be positive. An overlapping scheduled lesson for the same tutor returns `409 Lessons.TimeConflict`. An unknown lesson or one owned by another tutor returns `404`. Omitting `topic` keeps the saved topic. An empty value clears it. The topic is limited to 200 characters.

See the [reschedule implementation](../../../../../TutoringManagementSystem/Tutoring.Api/Features/Tutors/RescheduleLesson/) and the [Tutor workspace integration tests](../../../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/Tutors/TutorWorkspace/).
