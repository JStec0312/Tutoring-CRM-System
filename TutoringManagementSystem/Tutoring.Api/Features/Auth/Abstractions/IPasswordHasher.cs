namespace Tutoring.Api.Features.Auth.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool Verify(string hashedPassword, string providedPassword);
    
}