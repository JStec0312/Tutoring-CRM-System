using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Students.Exceptions;

public sealed class StudentInvitationUnavailableException
    : ConflictException
{
    public StudentInvitationUnavailableException()
        : base(
            "StudentInvitations.Unavailable",
            "Student invitation can no longer be accepted.")
    {
    }
}