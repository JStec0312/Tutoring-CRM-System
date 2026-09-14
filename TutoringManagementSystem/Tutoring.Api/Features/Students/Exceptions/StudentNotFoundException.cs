using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Students.Exceptions;

public sealed class StudentNotFoundException
    : NotFoundException
{
    public StudentNotFoundException(Guid userAccountId)
        : base(
            "Students.NotFound",
            $"Student for user account {userAccountId} was not found.")
    {
    }
}