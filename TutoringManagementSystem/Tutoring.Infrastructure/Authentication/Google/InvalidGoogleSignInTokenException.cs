namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class InvalidGoogleSignInTokenException()
    : Exception("The Google ID token is invalid.");
