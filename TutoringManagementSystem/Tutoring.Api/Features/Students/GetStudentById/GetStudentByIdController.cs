using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tutoring.Api.Features.Students.GetStudentById;


[ApiController]
[Route("api/students/{id:guid}")]
public class GetStudentByIdController(ISender sender) : ControllerBase
{
    // GET
    [HttpGet]
    public async Task<ActionResult<GetStudentByIdResponse>> GetStudentById(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetStudentByIdQuery(id), cancellationToken);
        return Ok(response);
    }
}