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
        try
        {
            var mailAddress = new MailAddress(value);

            return string.Equals(
                mailAddress.Address,
                value,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
