using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public readonly record struct LessonId(Guid Value)
    : DomainId<LessonId>
{
    public static LessonId New() => new(Guid.NewGuid());
}
