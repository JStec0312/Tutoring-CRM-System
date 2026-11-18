using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

public sealed record CompleteGoogleAccountLinkRequest(
    [Required, StringLength(64, MinimumLength = 64)] string ContinuationToken);
