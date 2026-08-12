using Tutoring.Domain.Common;

namespace Tutoring.UnitTest.Identity;

public class PhoneNumberTest
{
    [Theory]
    [InlineData("123456789")]
    [InlineData("+48123456789")]
    public void Create_withValidValue_ShouldSucceed(string value)
    {
        var phoneNumber = new PhoneNumber(value);

        Assert.Equal(value, phoneNumber.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_withInvalidValue_ShouldThrow(string value)
    {
        Assert.Throws<ArgumentException>(() => new PhoneNumber(value));
    }

    [Fact]
    public void Create_withValueContainingWhitespace_ShouldTrimValue()
    {
        var phoneNumber = new PhoneNumber("  123456789  ");

        Assert.Equal("123456789", phoneNumber.Value);
    }
}
