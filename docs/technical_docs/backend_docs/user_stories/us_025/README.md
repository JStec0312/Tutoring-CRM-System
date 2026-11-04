# US-025

> As a tutor, I want to remove outdated files so that the material library stays organized.

The backend endpoint and integration tests are implemented.

## API

`DELETE /api/learning-materials/{materialId}` accepts authenticated tutors and students.
A tutor can remove any material in an agreement they tutor. A student can remove only materials
they uploaded within an agreement in which they are the student.

- `204 No Content` when deletion is scheduled or the material is already awaiting cleanup.
- `401 Unauthorized` without a valid authenticated account.
- `403 Forbidden` for callers without a permitted role.
- `404 Not Found` with `LearningMaterials.ContextNotFound` for missing or inaccessible materials.

The API sets the material status to `Deleted` and records one cleanup message in the same database
transaction. Download and lesson attachment reject deleted materials. The outbox worker removes
the stored file and then removes its metadata, retrying storage failures on later passes.
An already absent file counts as successful cleanup. A request after metadata removal returns `404`.

[Deletion sequence](delete-learning-material.puml)

## Files

- [API slice](../../../../../TutoringManagementSystem/Tutoring.Api/Features/LearningMaterials/DeleteLearningMaterial)
- [Cleanup implementation](../../../../../TutoringManagementSystem/Tutoring.Infrastructure/FileStorage/LearningMaterialCleanup.cs)
- [Integration tests](../../../../../TutoringManagementSystem/Tutoring.IntegrationTests/Features/LearningMaterials/DeleteLearningMaterial/DeleteLearningMaterialTests.cs)
