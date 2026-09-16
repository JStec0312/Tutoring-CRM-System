using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class TutoringAgreementNotFoundException(
    Guid tutorId,
    Guid studentId)
    : NotFoundException(
        "TutoringAgreements.NotFound",
        $"Tutoring agreement for tutor {tutorId} and student {studentId} was not found.")
{
}
