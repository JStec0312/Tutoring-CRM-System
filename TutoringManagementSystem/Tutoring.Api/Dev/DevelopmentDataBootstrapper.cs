using Microsoft.EntityFrameworkCore;
using Tutoring.Domain.Billing;
using Tutoring.Domain.Common;
using Tutoring.Domain.Identity;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.Students;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Domain.Tutors;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Dev;

public sealed class DevelopmentDataBootstrapper(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<DevelopmentDataBootstrapper> logger)
{
    public const string Password = "Password123!";

    public static class Ids
    {
        // User Accounts
        public static readonly UserAccountId TutorAccount =
            new(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        public static readonly UserAccountId StudentAnnaAccount =
            new(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        public static readonly UserAccountId StudentPiotrAccount =
            new(Guid.Parse("00000000-0000-0000-0000-000000000003"));

        // Tutor
        public static readonly TutorId Tutor =
            new(Guid.Parse("00000000-0000-0000-0000-000000000101"));

        // Students
        public static readonly StudentId Anna =
            new(Guid.Parse("00000000-0000-0000-0000-000000000201"));

        public static readonly StudentId Piotr =
            new(Guid.Parse("00000000-0000-0000-0000-000000000202"));

        public static readonly StudentId ManualStudent =
            new(Guid.Parse("00000000-0000-0000-0000-000000000203"));

        // Agreements
        public static readonly TutoringAgreementId MathematicsAgreement =
            new(Guid.Parse("00000000-0000-0000-0000-000000000301"));

        public static readonly TutoringAgreementId PhysicsAgreement =
            new(Guid.Parse("00000000-0000-0000-0000-000000000302"));

        public static readonly TutoringAgreementId ProgrammingAgreement =
            new(Guid.Parse("00000000-0000-0000-0000-000000000303"));

        // Lessons
        public static readonly LessonId CompletedLesson =
            new(Guid.Parse("00000000-0000-0000-0000-000000000401"));

        public static readonly LessonId MissedLesson =
            new(Guid.Parse("00000000-0000-0000-0000-000000000402"));

        public static readonly LessonId CancelledLesson =
            new(Guid.Parse("00000000-0000-0000-0000-000000000403"));

        public static readonly LessonId UpcomingMathematicsLesson =
            new(Guid.Parse("00000000-0000-0000-0000-000000000404"));

        public static readonly LessonId UpcomingPhysicsLesson =
            new(Guid.Parse("00000000-0000-0000-0000-000000000405"));

        // Billing
        public static readonly BillingAccountId BillingAccount =
            new(Guid.Parse("00000000-0000-0000-0000-000000000501"));

        public static readonly LessonChargeId LessonCharge =
            new(Guid.Parse("00000000-0000-0000-0000-000000000502"));

    }

    public async Task BootstrapAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.UserAccounts
                .AsNoTracking()
                .AnyAsync(x => x.Id == Ids.TutorAccount, cancellationToken))
        {
            logger.LogInformation("Development data already bootstrapped.");
            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        var createdAtUtc = nowUtc.AddDays(-30);
        var passwordHash = passwordHasher.Hash(Password);

        // Accounts

        var tutorAccount = CreateAccount(
            Ids.TutorAccount,
            "tutor.dev@test.pl",
            "dev-tutor",
            "Development",
            "Tutor",
            UserRole.Tutor,
            passwordHash,
            createdAtUtc);

        var annaAccount = CreateAccount(
            Ids.StudentAnnaAccount,
            "student1.dev@test.pl",
            "dev-student-1",
            "Anna",
            "Nowak",
            UserRole.Student,
            passwordHash,
            createdAtUtc);

        var piotrAccount = CreateAccount(
            Ids.StudentPiotrAccount,
            "student2.dev@test.pl",
            "dev-student-2",
            "Piotr",
            "Kowalski",
            UserRole.Student,
            passwordHash,
            createdAtUtc);

        // Tutor

        var tutor = new Tutor(
            tutorAccount.Id,
            createdAtUtc);

        SetId(tutor, Ids.Tutor);

        // Students

        var anna = new Student(
            annaAccount.Id,
            new StudentDisplayName("Anna Nowak"),
            createdAtUtc);

        SetId(anna, Ids.Anna);

        var piotr = new Student(
            piotrAccount.Id,
            new StudentDisplayName("Piotr Kowalski"),
            createdAtUtc);

        SetId(piotr, Ids.Piotr);

        var manualStudent = new Student(
            new StudentDisplayName("Manual Student"),
            createdAtUtc);

        SetId(manualStudent, Ids.ManualStudent);

        var pln = new Currency("PLN");

        // Agreements

        var mathematicsAgreement = new TutoringAgreement(
            tutor.Id,
            anna.Id,
            new Subject("Mathematics"),
            new HourlyRate(new Money(100m, pln)),
            new AgreementTitle("Mathematics - Anna"),
            createdAtUtc);

        SetId(
            mathematicsAgreement,
            Ids.MathematicsAgreement);

        var physicsAgreement = new TutoringAgreement(
            tutor.Id,
            piotr.Id,
            new Subject("Physics"),
            new HourlyRate(new Money(120m, pln)),
            new AgreementTitle("Physics - Piotr"),
            createdAtUtc);

        SetId(
            physicsAgreement,
            Ids.PhysicsAgreement);

        var programmingAgreement = new TutoringAgreement(
            tutor.Id,
            manualStudent.Id,
            new Subject("Programming"),
            null,
            new AgreementTitle("Programming - Manual Student"),
            createdAtUtc);

        SetId(
            programmingAgreement,
            Ids.ProgrammingAgreement);

        programmingAgreement.Deactivate();

        // Lessons

        var completedLesson = CreateLesson(
            Ids.CompletedLesson,
            mathematicsAgreement.Id,
            nowUtc.AddDays(-7),
            TimeSpan.FromHours(1),
            createdAtUtc);

        completedLesson.Complete(nowUtc);

        var missedLesson = CreateLesson(
            Ids.MissedLesson,
            mathematicsAgreement.Id,
            nowUtc.AddDays(-4),
            TimeSpan.FromHours(1),
            createdAtUtc);

        missedLesson.MarkAsMissed(nowUtc);

        var cancelledLesson = CreateLesson(
            Ids.CancelledLesson,
            mathematicsAgreement.Id,
            nowUtc.AddDays(-2),
            TimeSpan.FromHours(1),
            createdAtUtc);

        cancelledLesson.Cancel(
            LessonCancellationParty.Student,
            new CancellationReason("Development sample cancellation"),
            annaAccount.Id,
            nowUtc.AddDays(-3));

        var upcomingMathematicsLesson = CreateLesson(
            Ids.UpcomingMathematicsLesson,
            mathematicsAgreement.Id,
            nowUtc.AddDays(2),
            TimeSpan.FromHours(1),
            createdAtUtc);

        var upcomingPhysicsLesson = CreateLesson(
            Ids.UpcomingPhysicsLesson,
            physicsAgreement.Id,
            nowUtc.AddDays(3),
            TimeSpan.FromMinutes(90),
            createdAtUtc);

        // Billing

        var billingAccount = new BillingAccount(
            mathematicsAgreement.Id,
            createdAtUtc);

        SetId(
            billingAccount,
            Ids.BillingAccount);

        var charge = billingAccount.AddLessonCharge(
            completedLesson.Id,
            mathematicsAgreement.HourlyRate!
                .CalculateCost(completedLesson.TimeSlot.Duration),
            completedLesson.TimeSlot.EndsAtUtc);

        SetId(charge, Ids.LessonCharge);

       
        dbContext.UserAccounts.AddRange(
            tutorAccount,
            annaAccount,
            piotrAccount);

        dbContext.Tutors.Add(tutor);

        dbContext.Students.AddRange(
            anna,
            piotr,
            manualStudent);

        dbContext.TutoringAgreements.AddRange(
            mathematicsAgreement,
            physicsAgreement,
            programmingAgreement);

        dbContext.Lessons.AddRange(
            completedLesson,
            missedLesson,
            cancelledLesson,
            upcomingMathematicsLesson,
            upcomingPhysicsLesson);

        dbContext.BillingAccounts.Add(billingAccount);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development data bootstrap completed.");
    }

    private UserAccount CreateAccount(
        UserAccountId id,
        string email,
        string username,
        string firstName,
        string lastName,
        UserRole role,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        var account = new UserAccount(
            new EmailAddress(email),
            new PasswordHash(passwordHash),
            new PersonalProfile(
                username,
                firstName,
                lastName,
                null),
            createdAtUtc);

        SetId(account, id);

        account.AssignRole(role);
        account.Activate();

        return account;
    }

    private Lesson CreateLesson(
        LessonId id,
        TutoringAgreementId agreementId,
        DateTimeOffset startsAtUtc,
        TimeSpan duration,
        DateTimeOffset createdAtUtc)
    {
        var lesson = new Lesson(
            agreementId,
            new TimeSlot(
                startsAtUtc,
                startsAtUtc.Add(duration)),
            createdAtUtc);

        SetId(lesson, id);

        return lesson;
    }

    private void SetId<TEntity, TId>(
        TEntity entity,
        TId id)
        where TEntity : Entity<TId>
        where TId : struct, DomainId<TId>
    {
        dbContext.Entry(entity)
            .Property(x => x.Id)
            .CurrentValue = id;
    }
}
