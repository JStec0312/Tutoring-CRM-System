namespace Tutoring.Api.Features.Students.Exceptions;
using Tutoring.Api.Features.Common.Exceptions;
public sealed class StudentNotFoundException : NotFoundException
{
    public StudentNotFoundException(Guid studentId)
        : base(
            code: "Students.NotFound",
            message: $"Student with id '{studentId}' was not found.")
    {
        StudentId = studentId;
    }

    public Guid StudentId { get; }
}

