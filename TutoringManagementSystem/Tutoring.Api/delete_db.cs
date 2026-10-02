namespace Tutoring.Api;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Development;
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

    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapDatabase(
        [FromServices] DevelopmentDataBootstrapper bootstrapper,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        await bootstrapper.BootstrapAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("reset-and-bootstrap")]
    public async Task<IActionResult> ResetAndBootstrapDatabase(
        [FromServices] TutoringDbContext dbContext,
        [FromServices] DevelopmentDataBootstrapper bootstrapper,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await bootstrapper.BootstrapAsync(cancellationToken);

        return NoContent();
    }
}
