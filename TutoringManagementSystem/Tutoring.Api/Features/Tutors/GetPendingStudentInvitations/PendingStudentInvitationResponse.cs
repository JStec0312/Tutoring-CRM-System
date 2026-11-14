namespace Tutoring.Api.Features.Tutors.GetPendingStudentInvitations;

public sealed record PendingStudentInvitationResponse(
    Guid InvitationId, string Email, string Subject, DateTimeOffset ValidUntilUtc);
