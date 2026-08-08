using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Domain.Identity;

public class PasswordHash
{
    private PasswordHash()
    {
    }

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new EmptyFieldException(nameof(PasswordHash));
        }


        Value = value;
    }
  
    public string Value { get; private set; } = null!;
    


    
}