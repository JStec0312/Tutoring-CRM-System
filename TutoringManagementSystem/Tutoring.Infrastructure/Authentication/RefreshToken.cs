using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public UserAccountId UserAccountId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public Guid FamilyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string CreatedByIp { get; private set; } = null!;
    public string CreatedByUserAgent { get; private set; } = null!;
    public bool IsRevoked => RevokedAtUtc.HasValue;

    public RefreshToken(
        UserAccountId userAccountId,
        string tokenHash,
        Guid familyId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        string? createdByIp = null,
        string? createdByUserAgent = null
        )
    {
        Id = Guid.NewGuid();
        UserAccountId = userAccountId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp ?? string.Empty;
        CreatedByUserAgent = createdByUserAgent ?? string.Empty;
    }

    public void Revoke(DateTime revokedAtUtc, Guid? replacedByTokenId = null)
    {
        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenId = replacedByTokenId;
    }

    public bool IsExpired(DateTime now ) => now >= ExpiresAtUtc;
}