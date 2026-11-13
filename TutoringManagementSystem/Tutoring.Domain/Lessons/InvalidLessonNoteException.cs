using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Lessons;

public sealed class InvalidLessonNoteException(string field, int maxLength)
    : DomainException("Lessons.InvalidNote", $"{field} must contain between 1 and {maxLength} characters.");
