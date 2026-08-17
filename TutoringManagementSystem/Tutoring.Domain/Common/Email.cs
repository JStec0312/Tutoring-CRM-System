using System.Net.Mail;
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Common;

public sealed record EmailAddress
{
    private EmailAddress()
    {
    }

    public EmailAddress(string value)
    {
        if(!IsValid(value) || string.IsNullOrWhiteSpace(value)) 
        {
            throw new InvalidEmailAddressException(value);
        }
        
        Value = value.Trim();
    }

    public string Value { get; private set; } = null!;

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var mailAddress = new MailAddress(value);

            if (!string.Equals(
                    mailAddress.Address,
                    value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var domain = value.Split('@').Last();

            return domain.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
