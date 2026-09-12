using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Common;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Mailing;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.RequestPasswordReset;

public sealed class RequestPasswordResetHandler(
    TutoringDbContext dbContext,
    IPasswordResetTokenGenerator passwordResetTokenGenerator,
    TimeProvider timeProvider,
    ILogger<RequestPasswordResetHandler> logger)
    : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(
        RequestPasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);

        var userAccount = await dbContext.UserAccounts
            .SingleOrDefaultAsync(
                user => user.Email.Value == email.Value,
                cancellationToken);

        if (userAccount is null)
        {
            logger.LogInformation(
                "Password reset requested for unknown email. RequestMetadata: {RequestMetadata}",
                request.Metadata);

            return;
        }

        if (!userAccount.IsActive)
        {
            logger.LogInformation(
                "Password reset ignored for inactive account. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
                userAccount.Id.Value,
                request.Metadata);

            return;
        }
        // deactivate old tokens
        var now = timeProvider.GetUtcNow();
        var activeTokens = await dbContext.PasswordResetTokens
            .Where(token =>
                token.UserAccountId == userAccount.Id &&
                token.UsedAtUtc == null &&
                token.InvalidatedAtUtc == null &&
                token.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Invalidate(now);
        }

        var generatedToken =
            passwordResetTokenGenerator.Generate(
                userAccount.Id,
                now);

        dbContext.PasswordResetTokens.Add(
            generatedToken.Token);

        var integrationEvent =
            new PasswordResetRequestedIntegrationEvent(
                UserId: userAccount.Id.Value,
                Email: userAccount.Email.Value,
                FirstName: userAccount.Profile.FirstName,
                ResetToken: generatedToken.Value);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type =PasswordResetRequestedIntegrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent),
            OccurredAtUtc = now,
            RetryCount = 0
        };

        dbContext.OutboxMessages.Add(outboxMessage);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Password reset event created. UserId: {UserId}, RequestMetadata: {RequestMetadata}",
            userAccount.Id.Value,
            request.Metadata);
    }
}