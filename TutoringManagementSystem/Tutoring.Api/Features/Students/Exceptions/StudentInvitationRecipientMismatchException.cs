using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Students.Exceptions;

public sealed class StudentInvitationRecipientMismatchException
    : ForbiddenException
{
    public StudentInvitationRecipientMismatchException()
        : base(
            "StudentInvitations.RecipientMismatch",
            "This invitation is assigned to another user.")
    {
    }
}