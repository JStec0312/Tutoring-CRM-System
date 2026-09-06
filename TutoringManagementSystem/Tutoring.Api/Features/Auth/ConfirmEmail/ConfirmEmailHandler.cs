using System.Security.Cryptography;
using MediatR;
using Tutoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Tutoring.Api.Features.Auth.Exceptions;

namespace Tutoring.Api.Features.Auth.ConfirmEmail;


public sealed class ConfirmEmailHandler(
    TutoringDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ConfirmEmailHandler> logger)
    : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {   
        logger.LogInformation("Handling email confirmation for token: {Token} from IP: {IpAddress} with User-Agent: {UserAgent} and TraceId: {TraceId}",
            request.Token,
            request.requestMetadata.IpAddress,
            request.requestMetadata.UserAgent,
            request.requestMetadata.TraceId);
        var tokenHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(request.Token)));

        var verificationToken =
            await dbContext.EmailVerificationTokens
                .SingleOrDefaultAsync(
                    x => x.TokenHash == tokenHash,
                    cancellationToken);

        if (verificationToken is null)
        {
            logger.LogWarning("Invalid email verification token for token: {Token} from IP: {IpAddress} with User-Agent: {UserAgent} and TraceId: {TraceId}",
                request.Token,
                request.requestMetadata.IpAddress,
                request.requestMetadata.UserAgent,
                request.requestMetadata.TraceId);
            throw new InvalidEmailVerificationTokenException();
        }

        var now = timeProvider.GetUtcNow();

        if (verificationToken.IsUsed ||
            verificationToken.ExpiresAtUtc <= now)
        {
            logger.LogWarning("Invalid email verification token for token: {Token} from IP: {IpAddress} with User-Agent: {UserAgent} and TraceId: {TraceId}",
                request.Token,
                request.requestMetadata.IpAddress,
                request.requestMetadata.UserAgent,
                request.requestMetadata.TraceId);
            throw new InvalidEmailVerificationTokenException();
        }

        var user = await dbContext.UserAccounts
            .SingleAsync(
                x => x.Id == verificationToken.UserAccountId,
                cancellationToken);

        user.Activate();

        verificationToken.MarkAsUsed(now);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}