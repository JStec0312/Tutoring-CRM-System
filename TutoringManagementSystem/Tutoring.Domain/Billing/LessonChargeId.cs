using Tutoring.Domain.Common;

namespace Tutoring.Domain.Billing;

public readonly record struct LessonChargeId(Guid Value)
    : IDomainId
{
    public static LessonChargeId New() => new(Guid.NewGuid());
}
