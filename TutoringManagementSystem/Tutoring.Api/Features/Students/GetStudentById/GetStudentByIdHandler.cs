using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Students.Exceptions;

namespace Tutoring.Api.Features.Students.GetStudentById;
using Infrastructure.Persistence;
using MediatR;

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