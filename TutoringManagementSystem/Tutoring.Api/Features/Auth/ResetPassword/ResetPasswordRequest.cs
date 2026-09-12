namespace Tutoring.Api.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);