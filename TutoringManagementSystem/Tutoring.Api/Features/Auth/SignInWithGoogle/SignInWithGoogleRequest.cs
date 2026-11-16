using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

public sealed record SignInWithGoogleRequest(
    [Required, StringLength(8192)] string IdToken,
    bool UseNativeSession = false);
