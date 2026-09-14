using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class StudentAlreadyAssignedException
    : ConflictException
{
    public StudentAlreadyAssignedException(string email)
        : base(
            "Students.AlreadyAssigned",
            $"Student {email} is already assigned to this tutor.")
    {
    }
}