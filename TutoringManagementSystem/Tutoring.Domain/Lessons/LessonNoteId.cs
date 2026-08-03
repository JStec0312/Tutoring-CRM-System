using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public readonly record struct LessonNoteId(Guid Value)
    : IDomainId
{
    public static LessonNoteId New() => new(Guid.NewGuid());
}
