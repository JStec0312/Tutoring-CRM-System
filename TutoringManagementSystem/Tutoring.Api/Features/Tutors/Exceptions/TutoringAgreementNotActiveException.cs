using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class TutoringAgreementNotActiveException(
    Guid tutoringAgreementId)
    : UseCaseException(
        "TutoringAgreements.NotActive",
        $"Tutoring agreement {tutoringAgreementId} is not active.")
{
}
