using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleSignInAccountLink(string googleSubject, UserAccountId accountId)
{
    public string GoogleSubject { get; } = googleSubject;
    public UserAccountId AccountId { get; } = accountId;
}
