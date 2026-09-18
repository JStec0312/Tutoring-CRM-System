
using Tutoring.Domain.Common;

namespace Tutoring.Domain.LessonSeries;

public readonly record struct LessonSeriesId(Guid Value)
    : DomainId<LessonSeriesId>
{
    public static LessonSeriesId New() => new(Guid.NewGuid());
}


