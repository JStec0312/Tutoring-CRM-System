namespace Tutoring.Api.Features.Lessons.Exceptions;

using System;
using Tutoring.Api.Features.Common.Exceptions;

public class LessonNotFoundForUserException : NotFoundException
{
    public LessonNotFoundForUserException(Guid lessonId) : base("Lessons.NotFoundForUser", $"Lesson with ID '{lessonId}' was not found for the user.")
    {
    }
}