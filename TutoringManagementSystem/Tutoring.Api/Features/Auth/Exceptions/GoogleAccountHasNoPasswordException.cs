using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public sealed class GoogleAccountHasNoPasswordException : ConflictException
{
    public GoogleAccountHasNoPasswordException()
        : base("Auth.GoogleAccountHasNoPassword",
            "This account is connected to Google and has no password. Sign in with Google.")
    {
    }
}
