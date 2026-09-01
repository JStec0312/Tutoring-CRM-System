using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using MediatR;
using Tutoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Domain.Students;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Mailing;
using System.Text.Json;

namespace Tutoring.Api.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPasswordPolicyValidator passwordPolicyValidator,
    TimeProvider timeProvider,
    ILogger<RegisterStudentHandler> logger
) : IRequestHandler<RegisterStudentCommand, RegisterStudentResponse>
{
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

        userAccount.AssignRole(UserRole.Student);

        var student = new Student(
            userAccountId: userAccount.Id,
            createdAtUtc: createdAtUtc);

        dbContext.UserAccounts.Add(userAccount);
        dbContext.Students.Add(student);
        var RegistrationEvent = new UserRegisteredIntegrationEvent(
            UserId: userAccount.Id.Value,
            Email: email.Value,
            FirstName: request.FirstName);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = UserRegisteredIntegrationEvent.EventType,
            Payload = JsonSerializer.Serialize(RegistrationEvent),
            OccurredAtUtc = createdAtUtc,
            RetryCount = 0
        };
        

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Registration succeeded. UserId: {UserId}, IP: {IpAddress}, TraceId: {TraceId}",
            userAccount.Id.Value,
            request.Metadata.IpAddress,
            request.Metadata.TraceId);

        return new RegisterStudentResponse(
            UserId: userAccount.Id.Value);
    }
}