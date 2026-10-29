# US-023

> Original story: As a tutor, I want to assign a file to a specific student so that I can share material with the right person.

## Status: Obsolete

A separate student material assignment workflow is not part of the current requirements.

The target design scopes each learning material to a tutoring agreement (TA), optionally to a lesson in that TA, and records the authenticated uploader. Both tutor and student participants may upload within that context. Role membership alone does not grant access to another TA or its lessons.

US-021 implements this upload context in the current persistence model. Legacy `MaterialAssignments` remains in persistence but is unused by upload. A separate sharing workflow and download/list access rules are not designed by these stories.

See [US-021](../us_021/README.md) for upload and participant authorization and [US-022](../us_022/README.md) for lesson context.
