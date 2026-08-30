using Microsoft.Extensions.Options;
using Tutoring.Domain.Common.Exceptions;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.UnitTest.Auth;

public class PasswordPolicyValidatorTests
{
    private static PasswordPolicyValidator CreateValidator(PasswordPolicyOptions? options = null)
    {
        var opt = Options.Create(options ?? new PasswordPolicyOptions());
        return new PasswordPolicyValidator(opt);
    }

    [Theory]
    [InlineData("Password123!")]
    [InlineData("Valid@1234")]
    [InlineData("Strong#Pass99")]
    public void Validate_WithValidPassword_ShouldNotThrow(string password)
    {
        var validator = CreateValidator();

        var exception = Record.Exception(() => validator.Validate(password));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespacePassword_ShouldThrowPasswordPolicyViolationException(string? password)
    {
        var validator = CreateValidator();

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate(password));
        Assert.Equal("Password cannot be empty.", ex.Message);
    }

    [Fact]
    public void Validate_WhenPasswordTooShort_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { MinimumLength = 8 });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("Pass1!"));
        Assert.Equal("Password must be at least 8 characters long.", ex.Message);
    }

    [Fact]
    public void Validate_WhenPasswordTooLong_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { MaximumLength = 10 });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("Password123456!"));
        Assert.Equal("Password must be at most 10 characters long.", ex.Message);
    }

    [Fact]
    public void Validate_WhenMissingUppercase_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { RequireUppercase = true });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("password123!"));
        Assert.Equal("Password must contain at least one uppercase letter.", ex.Message);
    }

    [Fact]
    public void Validate_WhenMissingLowercase_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { RequireLowercase = true });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("PASSWORD123!"));
        Assert.Equal("Password must contain at least one lowercase letter.", ex.Message);
    }

    [Fact]
    public void Validate_WhenMissingDigit_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { RequireDigit = true });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("Password!!"));
        Assert.Equal("Password must contain at least one digit.", ex.Message);
    }

    [Fact]
    public void Validate_WhenMissingSpecialCharacter_ShouldThrowPasswordPolicyViolationException()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { RequireSpecialCharacter = true });

        var ex = Assert.Throws<PasswordPolicyViolationException>(() => validator.Validate("Password123"));
        Assert.Equal("Password must contain at least one special character.", ex.Message);
    }
}
