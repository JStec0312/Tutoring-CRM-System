using MediatR;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.Exceptions;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Students.GetStudentById;

public sealed class GetStudentByIdHandler(
    TutoringDbContext dbContext)
    : IRequestHandler<GetStudentByIdQuery, GetStudentByIdResponse>
{
    public async Task<GetStudentByIdResponse> Handle(
        GetStudentByIdQuery request,
        CancellationToken cancellationToken)
    {
        // GetStudentByIdResponse? response = await dbContext.Students
        //     .AsNoTracking()
        //     .Where(student => student.Id == request.Id)
        //     .Select(student => new GetStudentByIdResponse(
        //         student.Id,
        //         student.FirstName,
        //         student.LastName,
        //         student.Email,
        //         student.PhoneNumber,
        //         student.CreatedAtUtc))
        //     .SingleOrDefaultAsync(cancellationToken);

        // if (response is null)
        // {
        //     throw new StudentNotFoundException(request.Id);
        // }

        throw new NotImplementedException();
    }
}