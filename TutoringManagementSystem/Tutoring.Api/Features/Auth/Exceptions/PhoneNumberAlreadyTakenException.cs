using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class PhoneNumberAlreadyTakenException : UseCaseException
{
    public PhoneNumberAlreadyTakenException(string phoneNumber)
        : base(
            "PhoneNumber.AlreadyTaken",
            $"The phone number '{phoneNumber}' is already taken.")
    {
    }
    
}