namespace Tutoring.Infrastructure.Authentication;

// Generated password reset token containing the plain value and the associated token entity.
// the token entity it corresponds to  a record stored in database  where the value is stored as a hash.
public sealed record GeneratedPasswordResetToken(
    string Value,
    PasswordResetToken Token);