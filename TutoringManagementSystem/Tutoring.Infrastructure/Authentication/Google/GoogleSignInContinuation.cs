using System.Security.Cryptography;
using System.Text;
using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleSignInContinuation(
    string tokenHash,
    string googleSubject,
    string email,
    UserAccountId? accountId,
    DateTimeOffset expiresAtUtc)
{
    public string TokenHash { get; } = tokenHash;
    public string GoogleSubject { get; } = googleSubject;
    public string Email { get; } = email;
    public UserAccountId? AccountId { get; } = accountId;
    public DateTimeOffset ExpiresAtUtc { get; } = expiresAtUtc;

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
