using Tutoring.Domain.Common;

namespace Tutoring.Domain.Students;

public readonly record struct StudentId(Guid Value)
    : IDomainId
{
    public static StudentId New() => new(Guid.NewGuid());
}
