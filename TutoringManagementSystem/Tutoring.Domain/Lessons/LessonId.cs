using Tutoring.Domain.Common;

namespace Tutoring.Domain.Lessons;

public readonly record struct LessonId(Guid Value)
    : IDomainId
{
    public static LessonId New() => new(Guid.NewGuid());
}
