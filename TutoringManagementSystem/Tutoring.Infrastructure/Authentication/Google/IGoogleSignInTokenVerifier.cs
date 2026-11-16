namespace Tutoring.Infrastructure.Authentication.Google;

public interface IGoogleSignInTokenVerifier
{
    Task<VerifiedGoogleSignInIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
