using Tutoring.Domain.Common;

namespace Tutoring.Domain.StudentInvitations;

public readonly record struct StudentInvitationId(Guid Value)
    : DomainId<StudentInvitationId>
{
    public static StudentInvitationId New() => new(Guid.NewGuid());
}
