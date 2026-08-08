using Microsoft.AspNetCore.Mvc;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api;
[ApiController]
public class ClearDb : Controller
{
    private readonly TutoringDbContext _dbContext;

    public ClearDb(TutoringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("/clear-db")]
    public IActionResult Index()
    {
        _dbContext.Students.RemoveRange(_dbContext.Students);
        _dbContext.UserAccounts.RemoveRange(_dbContext.UserAccounts);
        _dbContext.Tutors.RemoveRange(_dbContext.Tutors);
        _dbContext.BillingAccounts.RemoveRange(_dbContext.BillingAccounts);

        _dbContext.SaveChanges();
        return Ok("Database cleared.");
    }
}