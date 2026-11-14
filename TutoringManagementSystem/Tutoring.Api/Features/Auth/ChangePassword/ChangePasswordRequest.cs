using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Auth.ChangePassword;

public sealed record ChangePasswordRequest(
    [Required, StringLength(256)] string CurrentPassword,
    [Required, StringLength(256)] string NewPassword);
