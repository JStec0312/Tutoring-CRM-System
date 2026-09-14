using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Students.Exceptions;

public sealed class StudentInvitationNotFoundException
    : NotFoundException
{
    public StudentInvitationNotFoundException()
        : base(
            "StudentInvitations.NotFound",
            "Student invitation was not found.")
    {
    }
}