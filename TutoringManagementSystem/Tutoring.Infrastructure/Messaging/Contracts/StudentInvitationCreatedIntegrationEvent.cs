namespace Tutoring.Infrastructure.Messaging.Contracts;

public sealed record StudentInvitationCreatedIntegrationEvent(
    Guid InvitationId,
    string Email,
    string InvitationToken,
    string Title,
    string Subject,
    DateTimeOffset ValidUntilUtc)
{
    public const string EventType =
        "student-invitations.created.v1";
}