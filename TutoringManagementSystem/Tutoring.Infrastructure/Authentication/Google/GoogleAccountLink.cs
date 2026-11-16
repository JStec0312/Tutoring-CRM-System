using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication.Google;

public sealed record GoogleAccountLink(
    UserAccountId AccountId,
    string GoogleSubject);