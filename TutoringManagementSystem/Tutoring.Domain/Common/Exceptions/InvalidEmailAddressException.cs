namespace Tutoring.Domain.Common.Exceptions;

public class InvalidEmailAddressException : DomainException
{
    public InvalidEmailAddressException(string emailAddress)
        : base("Email.InvalidEmailAddressFormat", $"The email address '{emailAddress}' is invalid.")
    {
    }
}   