using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class EmailAlreadyTakenException : UseCaseException
{
    public EmailAlreadyTakenException(string email)
        : base(
            "Email.AlreadyTaken",
            $"The email '{email}' is already taken.")
    {
    }
    
}