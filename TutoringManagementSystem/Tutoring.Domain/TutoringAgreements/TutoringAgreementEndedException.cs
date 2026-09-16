using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.TutoringAgreements;

public sealed class TutoringAgreementEndedException : DomainException
{
    public TutoringAgreementEndedException(Guid agreementId)
        : base(
            "TutoringAgreements.Ended",
            $"Tutoring agreement {agreementId} has ended and its status can no longer be changed.")
    {
    }
}
