using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Users.Exceptions;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Users.GetProfile;

public sealed class GetProfileHandler(TutoringDbContext db) : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await db.UserAccounts.AsNoTracking()
            .Where(user => user.Id == request.AccountId)
            .Select(user => user.Profile)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UserAccountNotFoundException(request.AccountId.Value);

        return new ProfileResponse(profile.FirstName, profile.LastName, profile.PhoneNumber?.Value);
    }
}
