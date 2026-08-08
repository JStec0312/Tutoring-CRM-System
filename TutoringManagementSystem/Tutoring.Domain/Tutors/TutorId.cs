using Tutoring.Domain.Common;

namespace Tutoring.Domain.Tutors;

public readonly record struct TutorId(Guid Value)
    : DomainId<TutorId>
{
    public static TutorId New() => new(Guid.NewGuid());
}
