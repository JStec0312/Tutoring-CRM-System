namespace Tutoring.Infrastructure.Authentication.Google;

public interface IGoogleIdTokenVerifier
{
    Task<VerifiedGoogleIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
