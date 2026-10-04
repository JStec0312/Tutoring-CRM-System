using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.GetTutoringAgreementBalance;

public sealed class GetTutoringAgreementBalanceNotFoundException(
    Guid tutoringAgreementId)
    : NotFoundException(
        "TutoringAgreements.NotFound",
        $"Tutoring agreement {tutoringAgreementId} was not found.")
{
}
