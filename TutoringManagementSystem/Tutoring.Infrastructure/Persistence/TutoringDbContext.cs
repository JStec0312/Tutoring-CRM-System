using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.StudentInvitations;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Mailing;

namespace Tutoring.Infrastructure.Persistence;

public sealed class TutoringDbContext
    : DbContext
{
    public TutoringDbContext(
        DbContextOptions<TutoringDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<UserAccountRole> UserAccountRoles => Set<UserAccountRole>();

    public DbSet<Tutor> Tutors => Set<Tutor>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<TutoringAgreement> TutoringAgreements => Set<TutoringAgreement>();

    public DbSet<StudentInvitation> StudentInvitations => Set<StudentInvitation>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();

    public DbSet<BillingAccount> BillingAccounts => Set<BillingAccount>();

    public DbSet<LessonCharge> LessonCharges => Set<LessonCharge>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<LearningMaterial> LearningMaterials => Set<LearningMaterial>();

    public DbSet<MaterialAssignment> MaterialAssignments => Set<MaterialAssignment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TutoringDbContext).Assembly);
    }

    
}
