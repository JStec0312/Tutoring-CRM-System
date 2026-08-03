using MediatR;
using Tutoring.Domain.Students;

namespace Tutoring.Api.Features.Students.GetStudentById;

public record GetStudentByIdQuery(StudentId Id) : IRequest<GetStudentByIdResponse>;
