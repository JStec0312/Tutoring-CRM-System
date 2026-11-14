using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Tutors.GetPendingStudentInvitations;

public sealed record GetPendingStudentInvitationsQuery(UserAccountId AccountId)
    : IRequest<IReadOnlyList<PendingStudentInvitationResponse>>;
