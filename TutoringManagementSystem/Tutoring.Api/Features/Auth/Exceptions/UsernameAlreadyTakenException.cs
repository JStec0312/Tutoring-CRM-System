using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class UsernameAlreadyTakenException : ConflictException
{
    public UsernameAlreadyTakenException(string username)
        : base(
            "Username.AlreadyTaken",
            $"The username '{username}' is already taken.")
    {
    }
    
}