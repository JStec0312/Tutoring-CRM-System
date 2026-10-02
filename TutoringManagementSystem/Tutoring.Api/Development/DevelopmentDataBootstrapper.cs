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

namespace Tutoring.Api.Development;

public sealed class DevelopmentDataBootstrapper(
    TutoringDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<DevelopmentDataBootstrapper> logger)
{
    public const string TutorEmail = "tutor.dev@test.pl";
    public const string StudentOneEmail = "student1.dev@test.pl";
    public const string StudentTwoEmail = "student2.dev@test.pl";
    public const string DevelopmentPassword = "Password123!";

    public async Task BootstrapAsync(
        CancellationToken cancellationToken)
    {
        var alreadyBootstrapped = await dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                account => account.Email.Value == TutorEmail,
                cancellationToken);

        if (alreadyBootstrapped)
        {
            logger.LogInformation(
                "Development data bootstrap skipped because bootstrap tutor already exists. Email: {Email}",
                TutorEmail);

            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        var createdAtUtc = nowUtc.AddDays(-30);
        var passwordHash = passwordHasher.Hash(DevelopmentPassword);

        var tutorAccount = CreateAccount(
            TutorEmail,
            "dev-tutor",
            "Development",
            "Tutor",
            UserRole.Tutor,
            passwordHash,
            createdAtUtc);

        var studentOneAccount = CreateAccount(
            StudentOneEmail,
            "dev-student-1",
            "Anna",
            "Nowak",
            UserRole.Student,
            passwordHash,
            createdAtUtc);

        var studentTwoAccount = CreateAccount(
            StudentTwoEmail,
            "dev-student-2",
            "Piotr",
            "Kowalski",
            UserRole.Student,
            passwordHash,
            createdAtUtc);

        var tutor = new Tutor(
            tutorAccount.Id,
            createdAtUtc);

        var studentOne = new Student(
            studentOneAccount.Id,
            new StudentDisplayName("Anna Nowak"),
            createdAtUtc);

        var studentTwo = new Student(
            studentTwoAccount.Id,
            new StudentDisplayName("Piotr Kowalski"),
            createdAtUtc);

        var manualStudent = new Student(
            new StudentDisplayName("Manual Student"),
            createdAtUtc);

        var pln = new Currency("PLN");

        var mathematicsAgreement = new TutoringAgreement(
            tutor.Id,
            studentOne.Id,
            new Subject("Mathematics"),
            new HourlyRate(new Money(100m, pln)),
            new AgreementTitle("Mathematics - Anna"),
            createdAtUtc);

        mathematicsAgreement.UpdateTutoringAgreementDetails(
            subject: null,
            notes: "Main development agreement with lesson and billing history.");

        var physicsAgreement = new TutoringAgreement(
            tutor.Id,
            studentTwo.Id,
            new Subject("Physics"),
            new HourlyRate(new Money(120m, pln)),
            new AgreementTitle("Physics - Piotr"),
            createdAtUtc);

        var suspendedAgreement = new TutoringAgreement(
            tutor.Id,
            manualStudent.Id,
            new Subject("Programming"),
            hourlyRate: null,
            new AgreementTitle("Programming - manual student"),
            createdAtUtc);

        suspendedAgreement.Deactivate();

        var completedLesson = CreateLesson(
            mathematicsAgreement.Id,
            nowUtc.AddDays(-7).AddHours(-2),
            TimeSpan.FromHours(1),
            createdAtUtc);

        completedLesson.Complete(nowUtc);

        var missedLesson = CreateLesson(
            mathematicsAgreement.Id,
            nowUtc.AddDays(-4).AddHours(-2),
            TimeSpan.FromHours(1),
            createdAtUtc);

        missedLesson.MarkAsMissed(nowUtc);

        var cancelledLesson = CreateLesson(
            mathematicsAgreement.Id,
            nowUtc.AddDays(-2),
            TimeSpan.FromHours(1),
            createdAtUtc);

        cancelledLesson.Cancel(
            LessonCancellationParty.Student,
            new CancellationReason("Development sample cancellation"),
            studentOneAccount.Id,
            nowUtc.AddDays(-3));

        var upcomingMathematicsLesson = CreateLesson(
            mathematicsAgreement.Id,
            nowUtc.AddDays(2),
            TimeSpan.FromHours(1),
            nowUtc);

        var upcomingPhysicsLesson = CreateLesson(
            physicsAgreement.Id,
            nowUtc.AddDays(3),
            TimeSpan.FromMinutes(90),
            nowUtc);

        var billingAccount = new BillingAccount(
            mathematicsAgreement.Id,
            createdAtUtc);

        var completedLessonAmount =
            mathematicsAgreement.HourlyRate!.CalculateCost(
                completedLesson.TimeSlot.Duration);

        billingAccount.AddLessonCharge(
            completedLesson.Id,
            completedLessonAmount,
            completedLesson.TimeSlot.EndsAtUtc);

        billingAccount.RecordPayment(
            new Money(50m, pln),
            nowUtc.AddDays(-5),
            new PaymentReference("DEV-PAYMENT-001"));

        dbContext.UserAccounts.AddRange(
            tutorAccount,
            studentOneAccount,
            studentTwoAccount);

        dbContext.Tutors.Add(tutor);

        dbContext.Students.AddRange(
            studentOne,
            studentTwo,
            manualStudent);

        dbContext.TutoringAgreements.AddRange(
            mathematicsAgreement,
            physicsAgreement,
            suspendedAgreement);

        dbContext.Lessons.AddRange(
            completedLesson,
            missedLesson,
            cancelledLesson,
            upcomingMathematicsLesson,
            upcomingPhysicsLesson);

        dbContext.BillingAccounts.Add(billingAccount);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Development data bootstrap completed. TutorEmail: {TutorEmail}, Students: {StudentCount}, Agreements: {AgreementCount}, Lessons: {LessonCount}",
            TutorEmail,
            3,
            3,
            5);
    }

    private static UserAccount CreateAccount(
        string email,
        string userName,
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
                userName,
                firstName,
                lastName,
                phoneNumber: null),
            createdAtUtc);

        account.AssignRole(role);
        account.Activate();

        return account;
    }

    private static Lesson CreateLesson(
        TutoringAgreementId tutoringAgreementId,
        DateTimeOffset startsAtUtc,
        TimeSpan duration,
        DateTimeOffset createdAtUtc)
    {
        return new Lesson(
            tutoringAgreementId,
            new TimeSlot(
                startsAtUtc,
                startsAtUtc.Add(duration)),
            createdAtUtc);
    }
}
