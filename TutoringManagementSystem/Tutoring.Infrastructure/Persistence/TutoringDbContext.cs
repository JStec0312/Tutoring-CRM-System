using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Students;

namespace Tutoring.Infrastructure.Persistence;

public sealed class TutoringDbContext
    : DbContext
{
    public TutoringDbContext(
        DbContextOptions<TutoringDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TutoringDbContext).Assembly);
    }
}