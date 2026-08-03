using Tutoring.Domain.Common;

namespace Tutoring.Domain.Identity;

public readonly record struct UserAccountId(Guid Value)
    : IDomainId
{
    public static UserAccountId New() => new(Guid.NewGuid());
}
