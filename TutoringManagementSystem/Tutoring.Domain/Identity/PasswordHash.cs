namespace Tutoring.Domain.Identity;

public class PasswordHash
{
    private PasswordHash()
    {
    }

    public PasswordHash(string value)
    {
        throw new NotImplementedException("Password hashing is not implemented yet.");
    }

    public string Value { get; private set; } = null!;
    


    
}