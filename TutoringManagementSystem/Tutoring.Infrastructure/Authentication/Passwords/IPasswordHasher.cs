namespace Tutoring.Infrastructure.Authentication.Passwords;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}
