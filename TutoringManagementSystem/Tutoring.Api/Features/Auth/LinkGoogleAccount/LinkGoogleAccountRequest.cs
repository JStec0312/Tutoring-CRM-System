using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Auth.LinkGoogleAccount;

public sealed record LinkGoogleAccountRequest(
    [Required, StringLength(8192)] string IdToken);
