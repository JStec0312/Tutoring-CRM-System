using MediatR;
using Microsoft.EntityFrameworkCore;
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
    IPasswordPolicyValidator passwordPolicyValidator,
    TimeProvider timeProvider,
    ILogger<RegisterTutorHandler> logger)
    : IRequestHandler<RegisterTutorCommand, RegisterTutorResponse>
{
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

        passwordPolicyValidator.Validate(request.Password);

        var passwordHash = new PasswordHash(
            passwordHasher.Hash(request.Password));

        var profile = new PersonalProfile(
            userName: request.UserName,
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: phoneNumber);
        var createdAtUtc = timeProvider.GetUtcNow();

        var userAccount = new UserAccount(
            email: email,
            passwordHash: passwordHash,
            profile: profile,
            createdAtUtc: createdAtUtc);

        userAccount.AssignRole(UserRole.Tutor);

        var tutor = new Tutor(userAccount.Id, createdAtUtc);

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Tutors.Add(tutor);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Registration succeeded. UserId: {UserId}, IP: {IpAddress}, TraceId: {TraceId}",
            userAccount.Id.Value,
            request.Metadata.IpAddress,
            request.Metadata.TraceId);

        return new RegisterTutorResponse(
            userAccount.Id.Value);
    }
}