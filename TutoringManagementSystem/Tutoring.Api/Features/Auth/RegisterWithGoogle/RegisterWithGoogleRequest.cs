using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

public sealed record RegisterWithGoogleRequest(
    [Required, StringLength(64, MinimumLength = 64)] string ContinuationToken,
    [Required, RegularExpression("^(Tutor|Student)$")] string Role,
    [Required, StringLength(100)] string UserName,
    [StringLength(100)] string? FirstName = null,
    [StringLength(100)] string? LastName = null,
    [StringLength(30)] string? PhoneNumber = null,
    bool UseNativeSession = false);
