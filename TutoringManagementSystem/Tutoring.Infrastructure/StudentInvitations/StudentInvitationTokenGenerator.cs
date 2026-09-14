using System.Security.Cryptography;
using System.Text;

namespace Tutoring.Infrastructure.StudentInvitations;

public class StudentInvitationTokenGenerator : IStudentInvitationTokenGenerator
{
    public StudentInvitationToken Generate()
    {
        var value = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));

        var hash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));

        return new StudentInvitationToken(
            value,
            hash);
    }
}