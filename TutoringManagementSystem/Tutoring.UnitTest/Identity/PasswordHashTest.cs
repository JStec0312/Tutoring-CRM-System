using Tutoring.Domain.Common.Exceptions;
using Tutoring.Domain.Identity;

namespace Tutoring.UnitTest.Identity;

public class PasswordHashTest
{
    [Theory]
    [InlineData("hashed-value")]
    [InlineData("$2a$11$abcdefghijklmnopqrstuv")]
    public void Create_withValidValue_ShouldSucceed(string value)
    {
        var passwordHash = new PasswordHash(value);

        Assert.Equal(value, passwordHash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_withInvalidValue_ShouldThrow(string value)
    {
        Assert.Throws<EmptyFieldException>(() => new PasswordHash(value));
    }
}
