using System.Text.Json.Serialization;
using Tutoring.Api.Features.Auth.Login;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

public sealed record SignInWithGoogleResponse(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AccessToken = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? ExpiresAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RefreshToken = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? RefreshTokenExpiresAt = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ContinuationToken = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? ContinuationExpiresAtUtc = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Email = null)
{
    internal static SignInWithGoogleResponse FromSession(LoginHandlerResult session, bool useNativeSession) =>
        new(
            Status: "signedIn",
            AccessToken: session.AccessToken,
            ExpiresAt: session.ExpiresAt,
            RefreshToken: useNativeSession ? session.RefreshToken : null,
            RefreshTokenExpiresAt: useNativeSession ? session.RefreshTokenExpiresAt : null);
}
