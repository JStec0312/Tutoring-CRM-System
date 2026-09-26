namespace Tutoring.Api.Features.Users.ChangePassword;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
