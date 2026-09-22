using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonNotFoundException(
    Guid tutorId,
    Guid lessonId)
    : NotFoundException(
        "Lessons.NotFound",
        $"Lesson {lessonId} owned by tutor {tutorId} was not found.")
{
}