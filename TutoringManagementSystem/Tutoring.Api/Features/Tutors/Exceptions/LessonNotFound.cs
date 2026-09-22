using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonNotFoundException : NotFoundException
{
    public LessonNotFoundException(Guid tutorId, Guid lessonId) : base(
        "Lessons.LessonNotFound ",
        $"Lesson with ID '{lessonId}' for tutor with ID '{tutorId}' was not found.")
    {
    }
}