using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Users.Exceptions;

public sealed class InvalidCurrentPasswordException : AuthException
{
    public InvalidCurrentPasswordException()
        : base(
            "Users.InvalidCurrentPassword",
            "The current password is incorrect.")
    {
    }
}
