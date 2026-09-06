namespace Tutoring.Infrastructure.Authentication;

public sealed record GeneratedEmailVerificationToken(
    string Value,
    EmailVerificationToken Token);