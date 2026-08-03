using MediatR;

namespace Tutoring.Api.Features.Students.GetStudentById;

public record GetStudentByIdQuery(Guid Id) : IRequest<GetStudentByIdResponse>;
