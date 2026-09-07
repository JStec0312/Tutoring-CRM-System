using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Users.Exceptions;

public class UserAccountNotFoundException : NotFoundException
{
    public UserAccountNotFoundException(Guid userAccountId) : base("Users.NotFound", $"User with ID {userAccountId} not found.")
    {
    }
}