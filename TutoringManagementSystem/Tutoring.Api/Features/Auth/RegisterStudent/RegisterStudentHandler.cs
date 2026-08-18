using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using MediatR;
using Tutoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Students;
using Tutoring.Infrastructure.Abstraction;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher
) : IRequestHandler<RegisterStudentCommand, RegisterStudentResponse>
{
    private readonly int _passwordMinLength = AppSettings.PasswordPolicy.MinimumLength;
    private readonly int _passwordMaxLength = AppSettings.PasswordPolicy.MaximumLength;
    private readonly bool _passwordRequireUppercase = AppSettings.PasswordPolicy.RequireUppercase;
    private readonly bool _passwordRequireLowercase = AppSettings.PasswordPolicy.RequireLowercase;
    private readonly bool _passwordRequireDigit = AppSettings.PasswordPolicy.RequireDigit;
    private readonly bool _passwordRequireSpecialCharacter = AppSettings.PasswordPolicy.RequireSpecialCharacter;

    public async Task<RegisterStudentResponse> Handle(
        RegisterStudentCommand request,
        CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);

        var emailAlreadyExists = await dbContext.UserAccounts.AnyAsync(
            user => user.Email.Value == email.Value,
            cancellationToken);

        var userNameAlreadyExists = await dbContext.UserAccounts.AnyAsync(
            user => user.Profile.UserName == request.UserName,
            cancellationToken);

        if (userNameAlreadyExists)
        {
            throw new UsernameAlreadyTakenException(request.UserName);
        }

        if (emailAlreadyExists)
        {
            throw new EmailAlreadyTakenException(request.Email);
        }

        PhoneNumber? phoneNumber = null;

        if (request.PhoneNumber is not null)
        {
            phoneNumber = new PhoneNumber(request.PhoneNumber);

            var phoneNumberAlreadyExists = await dbContext.UserAccounts.AnyAsync(
                user => user.Profile.PhoneNumber == phoneNumber,
                cancellationToken);

            if (phoneNumberAlreadyExists)
            {
                throw new PhoneNumberAlreadyTakenException(request.PhoneNumber);
            }
        }

        var password = ValidatePassword(request.Password);

        var passwordHash = new PasswordHash(
            passwordHasher.Hash(password));

        var profile = new PersonalProfile(
            userName: request.UserName,
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: phoneNumber);

        var userAccount = new UserAccount(
            email: email,
            passwordHash: passwordHash,
            profile: profile);

        userAccount.AssignRole(UserRole.Student);

        var student = new Student(
            userAccountId: userAccount.Id);

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Students.Add(student);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterStudentResponse(
            UserId: userAccount.Id.Value);
    }

    private string ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordPolicyViolationException(
                "Password cannot be empty.");
        }

        if (password.Length < _passwordMinLength)
        {
            throw new PasswordPolicyViolationException(
                $"Password must be at least {_passwordMinLength} characters long.");
        }

        if (password.Length > _passwordMaxLength)
        {
            throw new PasswordPolicyViolationException(
                $"Password must be at most {_passwordMaxLength} characters long.");
        }

        if (_passwordRequireUppercase && !password.Any(char.IsUpper))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one uppercase letter.");
        }

        if (_passwordRequireLowercase && !password.Any(char.IsLower))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one lowercase letter.");
        }

        if (_passwordRequireDigit && !password.Any(char.IsDigit))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one digit.");
        }

        if (_passwordRequireSpecialCharacter &&
            !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new PasswordPolicyViolationException(
                "Password must contain at least one special character.");
        }

        return password;
    }
}