using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Tutoring.Infrastructure.Authentication;

public sealed class GoogleIdTokenVerifier(IConfiguration configuration)
{
    public async Task<string> VerifySubjectAsync(
        string idToken, CancellationToken cancellationToken)
    {
        var clientId = configuration["GoogleSignIn:ClientId"]?.Trim();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Google Web Client ID is missing.");
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };

        var payload = await GoogleJsonWebSignature
            .ValidateAsync(idToken, settings)
            .WaitAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new InvalidJwtException("The Google ID token is missing its subject.");
        }

        return payload.Subject;
    }
}