namespace Tutoring.Infrastructure.Authentication.EmailVerification;

public sealed record GeneratedEmailVerificationToken(
    string Value,
    EmailVerificationToken Token);