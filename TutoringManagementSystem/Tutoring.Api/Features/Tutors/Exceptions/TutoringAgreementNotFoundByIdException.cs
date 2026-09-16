using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class TutoringAgreementNotFoundByIdException(
    Guid tutorId,
    Guid tutoringAgreementId)
    : NotFoundException(
        "TutoringAgreements.NotFound",
        $"Tutoring agreement {tutoringAgreementId} owned by tutor {tutorId} was not found.")
{
}
