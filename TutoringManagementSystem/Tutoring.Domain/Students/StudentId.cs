using Tutoring.Domain.Common;

namespace Tutoring.Domain.Students;

public readonly record struct StudentId(Guid Value)
    : DomainId<StudentId>
{
    public static StudentId New() => new(Guid.NewGuid());
}
