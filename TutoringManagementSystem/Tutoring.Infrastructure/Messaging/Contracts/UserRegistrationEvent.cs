namespace Tutoring.Infrastructure.Messaging.Contracts;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string? FirstName)
{
    public const string EventType = "auth.user-registered.v1";
}