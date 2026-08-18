using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using MediatR;
using Tutoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler(
    TutoringDbContext dbContext
) : IRequestHandler<RegisterStudentCommand, RegisterStudentResponse>
{
    private readonly int PasswordMinLength = AppSettings.PasswordPolicy.MinimumLength;
    private readonly int PasswordMaxLength = AppSettings.PasswordPolicy.MaximumLength;
    private readonly bool PasswordRequireUppercase = AppSettings.PasswordPolicy.RequireUppercase;
    private readonly bool PasswordRequireLowercase = AppSettings.PasswordPolicy.RequireLowercase;
    private readonly bool PasswordRequireDigit = AppSettings.PasswordPolicy.RequireDigit;
    private readonly bool PasswordRequireSpecialCharacter = AppSettings.PasswordPolicy.RequireSpecialCharacter;


    public async Task<RegisterStudentResponse> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {   

        // validate email and username uniqueness

        var email = new EmailAddress(request.Email);
        
        var emailAlreadyExists = await dbContext.UserAccounts.AnyAsync(
            user => user.Email.Value == email.Value, cancellationToken);

        
        var userNameAlreadyExists = await dbContext.UserAccounts.AnyAsync(
            user => user.Profile.UserName == request.UserName, cancellationToken);
        

        if (userNameAlreadyExists)
        {
            throw new UsernameAlreadyTakenException(request.UserName);
        }

        if (emailAlreadyExists)
        {
            throw new EmailAlreadyTakenException(request.Email);
        }

        // validate phone number uniqueness if provided
        PhoneNumber? phoneNumber = null;
        if(request.PhoneNumber is not null)
        {
            phoneNumber = new PhoneNumber(request.PhoneNumber);
            var phoneNumberAlreadyExists = await dbContext.UserAccounts.AnyAsync(
                user => user.Profile.PhoneNumber == phoneNumber, cancellationToken);
            if (phoneNumberAlreadyExists)
            {
                throw new PhoneNumberAlreadyTakenException(request.PhoneNumber);
            }
        }

        // validate against password policy
        string password = ValidatePassword(request.Password);
        // hash password
        string passwordHashValue = BCrypt.Net.BCrypt.HashPassword(password);
        PasswordHash passwordHash = new PasswordHash(passwordHashValue);
        PersonalProfile profile = new PersonalProfile(
            userName: request.UserName,
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: phoneNumber
        );


        UserAccount userAccount = new UserAccount(
            email: email,
            passwordHash: passwordHash,
            profile: profile
        );
        UserRole studentRole = UserRole.Student;
        userAccount.AssignRole(studentRole);

        Student student = new Student(userAccountId: userAccount.Id);

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RegisterStudentResponse(
            UserId: userAccount.Id.Value
        );

    }

    private string ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new PasswordPolicyViolationException("Password cannot be empty.");
        }

        if (password.Length < PasswordMinLength)
        {
            throw new PasswordPolicyViolationException($"Password must be at least {PasswordMinLength} characters long.");
        }

        if (password.Length > PasswordMaxLength)
        {
            throw new PasswordPolicyViolationException($"Password must be at most {PasswordMaxLength} characters long.");
        }

        if (PasswordRequireUppercase && !password.Any(char.IsUpper))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one uppercase letter.");
        }

        if (PasswordRequireLowercase && !password.Any(char.IsLower))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one lowercase letter.");
        }

        if (PasswordRequireDigit && !password.Any(char.IsDigit))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one digit.");
        }

        if (PasswordRequireSpecialCharacter && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new PasswordPolicyViolationException("Password must contain at least one special character.");
        }

        return password;
    }
}
