using Microsoft.AspNetCore.Mvc;

namespace Tutoring.Api.Dev;

[ApiController]
[Route("api/dev/bootstrap")]
public sealed class DevelopmentBootstrapController(
    DevelopmentDataBootstrapper bootstrapper,
    IWebHostEnvironment environment,
    ILogger<DevelopmentBootstrapController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Bootstrap(
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        logger.LogInformation(
            "Development data bootstrap requested.");

        await bootstrapper.BootstrapAsync(
            cancellationToken);

        logger.LogInformation(
            "Development data bootstrap completed.");

        return NoContent();
    }
}