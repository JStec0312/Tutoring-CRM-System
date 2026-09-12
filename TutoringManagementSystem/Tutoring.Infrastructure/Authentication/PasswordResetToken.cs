using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public sealed class PasswordResetToken
{
    private PasswordResetToken()
    {
    }

    public Guid Id { get; private set; }

    public UserAccountId UserAccountId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public DateTimeOffset? InvalidatedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc is not null;

    public bool IsInvalidated => InvalidatedAtUtc is not null;

    public PasswordResetToken(
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

    public void Invalidate(DateTimeOffset invalidatedAtUtc)
    {
        InvalidatedAtUtc = invalidatedAtUtc;
    }
}