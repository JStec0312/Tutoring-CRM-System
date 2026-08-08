using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public readonly record struct LessonNoteId(Guid Value)
    : DomainId<LessonNoteId>
{
    public static LessonNoteId New() => new(Guid.NewGuid());
}
