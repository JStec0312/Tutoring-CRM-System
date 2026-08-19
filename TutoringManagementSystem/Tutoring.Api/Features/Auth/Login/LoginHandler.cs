using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.Login;

public sealed class LoginHandler(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(
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

        var passwordIsValid = passwordHasher.Verify(
            request.Password,
            userAccount.PasswordHash.Value);

        if (!passwordIsValid)
        {
            logger.LogWarning("Login failed for email: {Email} - invalid password", email);
            throw new InvalidCredentialsException();
        }

        var token = jwtTokenGenerator.Generate(userAccount);

        return new LoginResponse(
            AccessToken: token.Value,
            ExpiresAt: token.ExpiresAtUtc);
    }
}