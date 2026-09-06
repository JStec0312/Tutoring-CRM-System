using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public sealed class EmailVerificationToken
{
    private EmailVerificationToken()
    {
    }

    public Guid Id { get; private set; }

    public UserAccountId UserAccountId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc is not null;

    public EmailVerificationToken(
        UserAccountId userAccountId,
        string tokenHash,
        DateTimeOffset expiresAtUtc)
    {
        Id = Guid.NewGuid();
        UserAccountId = userAccountId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void MarkAsUsed(DateTimeOffset usedAtUtc)
    {
        UsedAtUtc = usedAtUtc;
    }
}