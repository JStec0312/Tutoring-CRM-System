namespace Tutoring.Domain.Common;
using FluentValidation;
using System.Net.Mail;
using Tutoring.Domain.Common.Exceptions;


public sealed record EmailAddress
{
    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IsValid(value))
        {
            throw new InvalidEmailAddressException(value);
        }

        string normalizedValue = value.Trim();

    
        Value = normalizedValue;
    }

    public string Value { get; }

    private static bool IsValid(string value)
    {
        try
        {
            var mailAddress = new MailAddress(value);

            return string.Equals(
                mailAddress.Address,
                value,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public override string ToString() => Value;
}