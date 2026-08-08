using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct LessonChargeId(Guid Value)
    : DomainId<LessonChargeId>
{
    public static LessonChargeId New() => new(Guid.NewGuid());
}
