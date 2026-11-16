using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

public sealed class InvalidGoogleRegistrationContinuationException()
    : AuthException("Auth.InvalidGoogleRegistrationContinuation", "The Google registration continuation is invalid or expired.");
