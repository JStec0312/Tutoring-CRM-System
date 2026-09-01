namespace Tutoring.Api;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Infrastructure.Persistence;

[ApiController]
[Route("api/dev/database")]
public sealed class ResetDatabaseEndpoint : ControllerBase
{
    [HttpDelete("reset")]
    public async Task<IActionResult> ResetDatabase(
        [FromServices] TutoringDbContext dbContext,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);

        await dbContext.Database.MigrateAsync(cancellationToken);

        return NoContent();
    }
}