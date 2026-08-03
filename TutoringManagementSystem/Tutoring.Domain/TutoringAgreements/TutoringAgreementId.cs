using Tutoring.Domain.Common;

namespace Tutoring.Domain.TutoringAgreements;

public readonly record struct TutoringAgreementId(Guid Value)
    : IDomainId
{
    public static TutoringAgreementId New() => new(Guid.NewGuid());
}
