using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tutoring.Api.Configuration;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Tutors;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.RegisterTutor;

public sealed class RegisterTutorHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions)
    : IRequestHandler<RegisterTutorCommand, RegisterTutorResponse>
{
    private readonly int _passwordMinLength = passwordPolicyOptions.Value.MinimumLength;
    private readonly int _passwordMaxLength = passwordPolicyOptions.Value.MaximumLength;
    private readonly bool _passwordRequireUppercase = passwordPolicyOptions.Value.RequireUppercase;
    private readonly bool _passwordRequireLowercase = passwordPolicyOptions.Value.RequireLowercase;
    private readonly bool _passwordRequireDigit = passwordPolicyOptions.Value.RequireDigit;
    private readonly bool _passwordRequireSpecialCharacter = passwordPolicyOptions.Value.RequireSpecialCharacter;

    public async Task<RegisterTutorResponse> Handle(
        RegisterTutorCommand request,
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

        userAccount.AssignRole(UserRole.Tutor);

        var tutor = new Tutor(userAccount.Id);

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Tutors.Add(tutor);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterTutorResponse(
            userAccount.Id.Value);
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