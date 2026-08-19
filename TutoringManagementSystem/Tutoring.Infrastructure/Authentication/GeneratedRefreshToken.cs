namespace Tutoring.Infrastructure.Authentication;

//<summary>
// Represents a generated refresh token along with its hashed value.
// It contains the original RefreshToken object and the hashed string value of the token.
// Refresh token is an object representing the record in the database, while the hashed value is used for secure storage and verification.
//</summary>
public sealed record GeneratedRefreshToken(
    RefreshToken Token,
    string Value
    );