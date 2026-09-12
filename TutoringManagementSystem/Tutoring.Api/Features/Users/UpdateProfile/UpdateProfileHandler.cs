using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Api.Features.Users.Exceptions;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Users.UpdateProfile;

public sealed class UpdateProfileHandler(
    TutoringDbContext dbContext,
    ILogger<UpdateProfileHandler> logger)
    : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userAccount = await dbContext.UserAccounts
            .SingleOrDefaultAsync(
                user => user.Id == request.UserAccountId,
                cancellationToken);

        if (userAccount is null)
        {
            logger.LogWarning(
                "User account not found. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                request.UserAccountId.Value,
                request.RequestMetadata);

            throw new UserAccountNotFoundException(
                request.UserAccountId.Value);
        }

        var userName = userAccount.Profile.UserName;

        if (request.UserName is not null)
        {
            var userNameAlreadyExists = await dbContext.UserAccounts
                .AnyAsync(
                    user =>
                        user.Id != request.UserAccountId &&
                        user.Profile.UserName == request.UserName,
                    cancellationToken);

            if (userNameAlreadyExists)
            {
                logger.LogWarning(
                    "Username already taken. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                    request.UserAccountId.Value,
                    request.RequestMetadata);

                throw new UsernameAlreadyTakenException(
                    request.UserName);
            }

            userName = request.UserName;
        }

        PhoneNumber? phoneNumber = null;

        if (request.PhoneNumber is not null)
        {
            phoneNumber = new PhoneNumber(request.PhoneNumber);

            var phoneNumberAlreadyExists = await dbContext.UserAccounts
                .AnyAsync(
                    user =>
                        user.Id != request.UserAccountId &&
                        user.Profile.PhoneNumber == phoneNumber,
                    cancellationToken);

            if (phoneNumberAlreadyExists)
            {
                logger.LogWarning(
                    "Phone number already taken. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                    request.UserAccountId.Value,
                    request.RequestMetadata);

                throw new PhoneNumberAlreadyTakenException(
                    request.PhoneNumber);
            }
        }

        var profile = new PersonalProfile(
            userName: userName,
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: phoneNumber);

        userAccount.UpdateProfile(profile);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Profile updated. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
            request.UserAccountId.Value,
            request.RequestMetadata);
    }
}