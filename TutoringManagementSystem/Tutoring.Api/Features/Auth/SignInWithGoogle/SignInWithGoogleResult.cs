using Tutoring.Api.Features.Auth.Login;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

public abstract record SignInWithGoogleResult
{
    // Indicates that the user has successfully signed in with their Google account.
    public sealed record SignedIn(LoginHandlerResult Session) : SignInWithGoogleResult;

    // Indicates that the user needs to continue the sign-in process, typically by linking their Google account.
    public sealed record Continue(bool RequiresLink, string Token, DateTimeOffset ExpiresAtUtc, string? Email) : SignInWithGoogleResult;
}
