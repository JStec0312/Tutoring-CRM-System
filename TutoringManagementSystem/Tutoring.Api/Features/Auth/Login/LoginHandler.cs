using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Infrastructure.Authentication.Passwords;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.Login;

public sealed class LoginHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    LoginSessionIssuer sessionIssuer,
    ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, LoginHandlerResult>
{
    public async Task<LoginHandlerResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var userAccount = await dbContext.UserAccounts
        .Include(user => user.RoleAssignments)
        .AsNoTracking()
        .SingleOrDefaultAsync(
        user => user.Email.Value == email,
        cancellationToken);

        if (userAccount is null)
        {
            logger.LogWarning("Login failed for email: {Email} - user not found", email);
            throw new InvalidCredentialsException();
        }

        if (userAccount.IsActive && userAccount.PasswordHash is null &&
            await dbContext.GoogleSignInAccountLinks.AnyAsync(
                link => link.AccountId == userAccount.Id, cancellationToken))
        {
            throw new GoogleAccountHasNoPasswordException();
        }

        var passwordIsValid = userAccount.PasswordHash is not null && passwordHasher.Verify(
            request.Password,
            userAccount.PasswordHash.Value);

        if (!passwordIsValid)
        {
            logger.LogWarning("Login failed for email: {Email} - invalid password", email);
            throw new InvalidCredentialsException();
        }
        if(!userAccount.IsActive)
        {
            logger.LogWarning("Login failed for email: {Email} - account is disabled", email);
            throw new InactiveAccountException();
        }
        var session = sessionIssuer.Issue(userAccount, request.Metadata);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }
}