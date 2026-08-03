using Tutoring.Domain.Common;

namespace Tutoring.Domain.Tutors;

public readonly record struct TutorId(Guid Value)
    : IDomainId
{
    public static TutorId New() => new(Guid.NewGuid());
}
