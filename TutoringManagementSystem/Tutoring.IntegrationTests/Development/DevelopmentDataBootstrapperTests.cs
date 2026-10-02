using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tutoring.Api.Development;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Development;

[Collection(IntegrationTestCollection.Name)]
public sealed class DevelopmentDataBootstrapperTests(
    IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task BootstrapAsync_ShouldCreateReusableDevelopmentScenario()
    {
        await BootstrapAsync();

        var snapshot = await ExecuteDbAsync(async dbContext =>
            new BootstrapSnapshot(
                await dbContext.UserAccounts.CountAsync(),
                await dbContext.UserAccountRoles.CountAsync(),
                await dbContext.Tutors.CountAsync(),
                await dbContext.Students.CountAsync(),
                await dbContext.TutoringAgreements.CountAsync(),
                await dbContext.TutoringAgreements.CountAsync(
                    agreement => agreement.Status == AgreementStatus.Active),
                await dbContext.TutoringAgreements.CountAsync(
                    agreement => agreement.Status == AgreementStatus.Suspended),
                await dbContext.Lessons.CountAsync(),
                await dbContext.Lessons.CountAsync(
                    lesson => lesson.Status == LessonStatus.Scheduled),
                await dbContext.Lessons.CountAsync(
                    lesson => lesson.Status == LessonStatus.Completed),
                await dbContext.Lessons.CountAsync(
                    lesson => lesson.Status == LessonStatus.Missed),
                await dbContext.Lessons.CountAsync(
                    lesson => lesson.Status == LessonStatus.Cancelled),
                await dbContext.BillingAccounts.CountAsync(),
                await dbContext.LessonCharges.CountAsync(),
                await dbContext.Payments.CountAsync()));

        Assert.Equal(3, snapshot.UserAccounts);
        Assert.Equal(3, snapshot.UserAccountRoles);
        Assert.Equal(1, snapshot.Tutors);
        Assert.Equal(3, snapshot.Students);
        Assert.Equal(3, snapshot.Agreements);
        Assert.Equal(2, snapshot.ActiveAgreements);
        Assert.Equal(1, snapshot.SuspendedAgreements);
        Assert.Equal(5, snapshot.Lessons);
        Assert.Equal(2, snapshot.ScheduledLessons);
        Assert.Equal(1, snapshot.CompletedLessons);
        Assert.Equal(1, snapshot.MissedLessons);
        Assert.Equal(1, snapshot.CancelledLessons);
        Assert.Equal(1, snapshot.BillingAccounts);
        Assert.Equal(1, snapshot.LessonCharges);
        Assert.Equal(1, snapshot.Payments);

        var session = await LoginAsync(
            DevelopmentDataBootstrapper.TutorEmail,
            DevelopmentDataBootstrapper.DevelopmentPassword);

        Assert.False(string.IsNullOrWhiteSpace(session.Login.AccessToken));
    }

    [Fact]
    public async Task BootstrapAsync_WhenCalledTwice_ShouldNotDuplicateData()
    {
        await BootstrapAsync();
        await BootstrapAsync();

        var counts = await ExecuteDbAsync(async dbContext =>
            new
            {
                UserAccounts = await dbContext.UserAccounts.CountAsync(),
                Students = await dbContext.Students.CountAsync(),
                Agreements = await dbContext.TutoringAgreements.CountAsync(),
                Lessons = await dbContext.Lessons.CountAsync(),
                BillingAccounts = await dbContext.BillingAccounts.CountAsync(),
                LessonCharges = await dbContext.LessonCharges.CountAsync(),
                Payments = await dbContext.Payments.CountAsync()
            });

        Assert.Equal(3, counts.UserAccounts);
        Assert.Equal(3, counts.Students);
        Assert.Equal(3, counts.Agreements);
        Assert.Equal(5, counts.Lessons);
        Assert.Equal(1, counts.BillingAccounts);
        Assert.Equal(1, counts.LessonCharges);
        Assert.Equal(1, counts.Payments);
    }

    [Fact]
    public async Task BootstrapEndpoint_OutsideDevelopment_ShouldReturnNotFound()
    {
        var response = await Client.PostAsync(
            "/api/dev/database/bootstrap",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task BootstrapAsync()
    {
        using var scope = Fixture.Services.CreateScope();

        var bootstrapper = scope.ServiceProvider
            .GetRequiredService<DevelopmentDataBootstrapper>();

        await bootstrapper.BootstrapAsync(CancellationToken.None);
    }

    private sealed record BootstrapSnapshot(
        int UserAccounts,
        int UserAccountRoles,
        int Tutors,
        int Students,
        int Agreements,
        int ActiveAgreements,
        int SuspendedAgreements,
        int Lessons,
        int ScheduledLessons,
        int CompletedLessons,
        int MissedLessons,
        int CancelledLessons,
        int BillingAccounts,
        int LessonCharges,
        int Payments);
}
