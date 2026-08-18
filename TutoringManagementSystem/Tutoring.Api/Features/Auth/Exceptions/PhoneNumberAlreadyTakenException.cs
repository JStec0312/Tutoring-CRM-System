using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class PhoneNumberAlreadyTakenException : ConflictException
{
    public PhoneNumberAlreadyTakenException(string phoneNumber)
        : base(
            "PhoneNumber.AlreadyTaken",
            $"The phone number '{phoneNumber}' is already taken.")
    {
    }
    
}