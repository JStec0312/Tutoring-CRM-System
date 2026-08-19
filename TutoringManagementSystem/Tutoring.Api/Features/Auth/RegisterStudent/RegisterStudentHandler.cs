using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using MediatR;
using Tutoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tutoring.Api.Configuration;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Students;
using Tutoring.Infrastructure.Authentication;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<PasswordPolicyOptions> passwordPolicyOptions,
    ILogger<RegisterStudentHandler> logger
) : IRequestHandler<RegisterStudentCommand, RegisterStudentResponse>
{
    private readonly int _passwordMinLength = passwordPolicyOptions.Value.MinimumLength;
    private readonly int _passwordMaxLength = passwordPolicyOptions.Value.MaximumLength;
    private readonly bool _passwordRequireUppercase = passwordPolicyOptions.Value.RequireUppercase;
    private readonly bool _passwordRequireLowercase = passwordPolicyOptions.Value.RequireLowercase;
    private readonly bool _passwordRequireDigit = passwordPolicyOptions.Value.RequireDigit;
    private readonly bool _passwordRequireSpecialCharacter = passwordPolicyOptions.Value.RequireSpecialCharacter;

    public async Task<RegisterStudentResponse> Handle(
        RegisterStudentCommand request,
        CancellationToken cancellationToken
        )
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
            logger.LogWarning(
                "Registration failed. Email: {Email}, UserName: {UserName}, IP: {IpAddress}, UserAgent: {UserAgent}, Reason: {Reason}, TraceId: {TraceId}",
                request.Email,
                request.UserName,
                request.Metadata.IpAddress,
                request.Metadata.UserAgent,
                "UsernameAlreadyTaken",
                request.Metadata.TraceId);
            throw new UsernameAlreadyTakenException(request.UserName);
        }

        if (emailAlreadyExists)
        {
            logger.LogWarning(
                "Registration failed. Email: {Email}, IP: {IpAddress}, UserAgent: {UserAgent}, Reason: {Reason}, TraceId: {TraceId}",
                request.Email,
                request.Metadata.IpAddress,
                request.Metadata.UserAgent,
                "EmailAlreadyTaken",
                request.Metadata.TraceId);
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
                logger.LogWarning(
                    "Registration failed. Email: {Email}, PhoneNumber: {PhoneNumber}, IP: {IpAddress}, UserAgent: {UserAgent}, Reason: {Reason}, TraceId: {TraceId}",
                    request.Email,
                    request.PhoneNumber,
                    request.Metadata.IpAddress,
                    request.Metadata.UserAgent,
                    "PhoneNumberAlreadyTaken",
                    request.Metadata.TraceId);
                throw new PhoneNumberAlreadyTakenException(request.PhoneNumber);
            }
        }

        var password = ValidatePassword(request);

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

        logger.LogInformation(
            "Registration succeeded. UserId: {UserId}, IP: {IpAddress}, TraceId: {TraceId}",
            userAccount.Id.Value,
            request.Metadata.IpAddress,
            request.Metadata.TraceId);

        return new RegisterStudentResponse(
            UserId: userAccount.Id.Value);
    }


    private string ValidatePassword(RegisterStudentCommand request)
    {
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(password))
        {
            LogPasswordPolicyViolation(request, "Password cannot be empty.");
            throw new PasswordPolicyViolationException(
                "Password cannot be empty.");
        }

        if (password.Length < _passwordMinLength)
        {
            LogPasswordPolicyViolation(request, $"Password must be at least {_passwordMinLength} characters long.");
            throw new PasswordPolicyViolationException(
                $"Password must be at least {_passwordMinLength} characters long.");
        }

        if (password.Length > _passwordMaxLength)
        {
            LogPasswordPolicyViolation(request, $"Password must be at most {_passwordMaxLength} characters long.");
            throw new PasswordPolicyViolationException(
                $"Password must be at most {_passwordMaxLength} characters long.");
        }

        if (_passwordRequireUppercase && !password.Any(char.IsUpper))
        {
            LogPasswordPolicyViolation(request, "Password must contain at least one uppercase letter.");
            throw new PasswordPolicyViolationException(
                "Password must contain at least one uppercase letter.");
        }

        if (_passwordRequireLowercase && !password.Any(char.IsLower))
        {
            LogPasswordPolicyViolation(request, "Password must contain at least one lowercase letter.");
            throw new PasswordPolicyViolationException(
                "Password must contain at least one lowercase letter.");
        }

        if (_passwordRequireDigit && !password.Any(char.IsDigit))
        {
            LogPasswordPolicyViolation(request, "Password must contain at least one digit.");
            throw new PasswordPolicyViolationException(
                "Password must contain at least one digit.");
        }

        if (_passwordRequireSpecialCharacter &&
            !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            LogPasswordPolicyViolation(request, "Password must contain at least one special character.");
            throw new PasswordPolicyViolationException(
                "Password must contain at least one special character.");
        }

        return password;
    }

    private void LogPasswordPolicyViolation(RegisterStudentCommand request, string detail)
    {
        logger.LogWarning(
            "Registration failed. Email: {Email}, IP: {IpAddress}, UserAgent: {UserAgent}, Reason: {Reason}, Detail: {Detail}, TraceId: {TraceId}",
            request.Email,
            request.Metadata.IpAddress,
            request.Metadata.UserAgent,
            "PasswordPolicyViolation",
            detail,
            request.Metadata.TraceId);
    }
}