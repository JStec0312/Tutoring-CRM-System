using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class TutorNotFoundException : NotFoundException
{
    public TutorNotFoundException(Guid userAccountId)
        : base(
            "Tutors.NotFound",
            $"Tutor for user account {userAccountId} was not found.")
    {
    }
}