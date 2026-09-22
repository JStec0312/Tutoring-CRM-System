namespace Tutoring.Api.Features.Lessons.Exceptions;

using System;
using Tutoring.Api.Features.Common.Exceptions;

public class CanNotCancelLessonAsATutorBeingStudentException : AuthException
{
    public CanNotCancelLessonAsATutorBeingStudentException(Guid lessonId) : base("Lessons.CanNotCancelAsTutorBeingStudent", $"Cannot cancel lesson with ID '{lessonId}' as a tutor while being a student.")
    {
    }
}