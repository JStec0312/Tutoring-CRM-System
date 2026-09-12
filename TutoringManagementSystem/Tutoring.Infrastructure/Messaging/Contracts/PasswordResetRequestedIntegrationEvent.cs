namespace Tutoring.Infrastructure.Messaging.Contracts;

public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string? FirstName,
    string ResetToken)
{
    public const string EventType =
        "auth.password-reset-requested.v1";
}