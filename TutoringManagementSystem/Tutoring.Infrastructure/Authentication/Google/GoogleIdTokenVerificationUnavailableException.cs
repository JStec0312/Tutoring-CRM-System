namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleIdTokenVerificationUnavailableException()
    : Exception("The API could not verify the Google ID token. Try again later.");
