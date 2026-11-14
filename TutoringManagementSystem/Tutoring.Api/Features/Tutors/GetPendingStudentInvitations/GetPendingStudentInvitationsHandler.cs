using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.GetPendingStudentInvitations;

public sealed class GetPendingStudentInvitationsHandler(TutoringDbContext db, TimeProvider clock)
    : IRequestHandler<GetPendingStudentInvitationsQuery, IReadOnlyList<PendingStudentInvitationResponse>>
{
    public async Task<IReadOnlyList<PendingStudentInvitationResponse>> Handle(
        GetPendingStudentInvitationsQuery request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        return await db.StudentInvitations.AsNoTracking()
            .Where(invitation => invitation.Tutor.UserAccountId == request.AccountId &&
                invitation.ValidUntilUtc > now &&
                (invitation.Status == InvitationStatus.Created || invitation.Status == InvitationStatus.Sent))
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .Select(invitation => new PendingStudentInvitationResponse(
                invitation.Id.Value, invitation.Recipient.Value, invitation.Subject.Name, invitation.ValidUntilUtc))
            .ToListAsync(cancellationToken);
    }
}
