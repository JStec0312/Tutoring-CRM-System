using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

public sealed class InvalidGoogleLinkContinuationException()
    : AuthException("Auth.InvalidGoogleLinkContinuation", "The Google linking continuation is invalid or expired.");
