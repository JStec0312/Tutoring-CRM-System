using Tutoring.Domain.Identity;

namespace Tutoring.Infrastructure.Authentication;

public sealed record GoogleAccountLink(
    UserAccountId AccountId,
    string GoogleSubject);