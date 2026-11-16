using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using JsonWebToken = Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;

namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleIdTokenVerifier(IConfiguration configuration) : IGoogleIdTokenVerifier
{
    public async Task<VerifiedGoogleIdentity> VerifyAsync(
        string idToken, CancellationToken cancellationToken)
    {
        var clientId = configuration["GoogleSignIn:ClientId"]?.Trim();

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new GoogleIdTokenVerificationUnavailableException();
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };

        try
        {
            var payload = await GoogleJsonWebSignature
                .ValidateAsync(idToken, settings)
                .WaitAsync(cancellationToken);

            var subject = payload.Subject;
            if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
            {
                throw new InvalidJwtException("The Google ID token has an invalid subject.");
            }

            var jwt = new JsonWebToken(idToken);
            if (!jwt.TryGetPayloadValue<string>("nonce", out var nonce) ||
                nonce is not { Length: 64 })
            {
                throw new InvalidJwtException("The Google ID token has an invalid nonce.");
            }

            return new VerifiedGoogleIdentity(subject, nonce);
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new GoogleIdTokenVerificationUnavailableException();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or JsonException or SecurityTokenException)
        {
            throw new InvalidJwtException("The Google ID token is invalid.");
        }
    }
}