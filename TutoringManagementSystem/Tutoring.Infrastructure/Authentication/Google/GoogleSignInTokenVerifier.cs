using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using JsonWebToken = Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Tutoring.Infrastructure.Authentication.Google;

public sealed class GoogleSignInTokenVerifier(IConfiguration configuration) : IGoogleSignInTokenVerifier
{
    public async Task<VerifiedGoogleSignInIdentity> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        var clientId = configuration["GoogleSignIn:ClientId"]?.Trim();
        if (string.IsNullOrEmpty(clientId))
            throw new GoogleSignInVerificationUnavailableException();

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken, new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] })
                .WaitAsync(cancellationToken);

            var jwt = new JsonWebToken(idToken);
            if (string.IsNullOrWhiteSpace(payload.Subject) || payload.Subject.Length > 255 ||
                !jwt.TryGetPayloadValue<string>("nonce", out var nonce) ||
                nonce is not { Length: 64 } || !nonce.All(Uri.IsHexDigit))
            {
                throw new InvalidGoogleSignInTokenException();
            }

            var email = payload.EmailVerified ? payload.Email?.Trim() : null;
            return new VerifiedGoogleSignInIdentity(payload.Subject, nonce, email);
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new GoogleSignInVerificationUnavailableException();
        }
        catch (Exception exception) when (exception is InvalidJwtException or FormatException or
            ArgumentException or JsonException or SecurityTokenException)
        {
            throw new InvalidGoogleSignInTokenException();
        }
    }
}
