using Tutoring.Domain.Common;

namespace Tutoring.Domain.Identity;

public readonly record struct UserAccountId(Guid Value)
    : DomainId<UserAccountId>
{
    public static UserAccountId New()
        => new(Guid.NewGuid());
}