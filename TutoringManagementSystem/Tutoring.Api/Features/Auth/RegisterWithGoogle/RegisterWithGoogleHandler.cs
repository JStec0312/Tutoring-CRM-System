using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Auth.Exceptions;
using Tutoring.Api.Features.Auth.Login;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Students;
using Tutoring.Domain.Tutors;
using Tutoring.Infrastructure.Authentication.Google;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

public sealed class RegisterWithGoogleHandler(
    TutoringDbContext dbContext, LoginSessionIssuer sessionIssuer, TimeProvider timeProvider)
    : IRequestHandler<RegisterWithGoogleCommand, LoginHandlerResult>
{
    public async Task<LoginHandlerResult> Handle(
        RegisterWithGoogleCommand request, CancellationToken cancellationToken)
    {
        var role = request.Role switch
        {
            "Tutor" => UserRole.Tutor,
            "Student" => UserRole.Student,
            _ => throw new ArgumentException("Select the Tutor or Student role.", nameof(request.Role))
        };
        var tokenHash = GoogleSignInContinuation.HashToken(request.ContinuationToken);
        var now = timeProvider.GetUtcNow();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var continuation = await dbContext.GoogleSignInContinuations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash &&
                item.AccountId == null && item.ExpiresAtUtc > now, cancellationToken);
        if (continuation is null)
        {
            throw new InvalidGoogleRegistrationContinuationException();
        }

        var deleted = await dbContext.GoogleSignInContinuations
            .Where(item => item.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted != 1)
        {
            throw new InvalidGoogleRegistrationContinuationException();
        }

        if (await dbContext.UserAccounts.AnyAsync(user => user.Email.Value == continuation.Email, cancellationToken) ||
            await dbContext.GoogleSignInAccountLinks.AnyAsync(link => link.GoogleSubject == continuation.GoogleSubject, cancellationToken))
        {
            throw new GoogleRegistrationConflictException();
        }

        var userName = request.UserName.Trim();
        if (await dbContext.UserAccounts.AnyAsync(user => user.Profile.UserName == userName, cancellationToken))
        {
            throw new UsernameAlreadyTakenException(userName);
        }

        PhoneNumber? phoneNumber = null;
        if (request.PhoneNumber is not null)
        {
            phoneNumber = new PhoneNumber(request.PhoneNumber);
            if (await dbContext.UserAccounts.AnyAsync(user => user.Profile.PhoneNumber == phoneNumber, cancellationToken))
            {
                throw new PhoneNumberAlreadyTakenException(request.PhoneNumber);
            }
        }

        var profile = new PersonalProfile(userName, request.FirstName, request.LastName, phoneNumber);
        var account = new UserAccount(new EmailAddress(continuation.Email), passwordHash: null, profile, now);
        account.AssignRole(role);
        account.Activate();
        dbContext.UserAccounts.Add(account);

        if (role == UserRole.Tutor)
        {
            dbContext.Tutors.Add(new Tutor(account.Id, now));
        }
        else
        {
            dbContext.Students.Add(new Student(account.Id, new StudentDisplayName(userName), now));
        }

        dbContext.GoogleSignInAccountLinks.Add(new GoogleSignInAccountLink(continuation.GoogleSubject, account.Id));
        var session = sessionIssuer.Issue(account, request.Metadata);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new GoogleRegistrationConflictException();
        }

        await transaction.CommitAsync(cancellationToken);
        return session;
    }
}
