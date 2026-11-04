using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.Lessons;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.LearningMaterials.DownloadLearningMaterial;

[Collection(IntegrationTestCollection.Name)]
public sealed class DownloadLearningMaterialTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    private const string FileName = "Lesson notes.pdf";
    private const string ContentType = "application/pdf";
    private static readonly byte[] FileBytes = "%PDF-1.7\nLesson notes"u8.ToArray();

    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow;

    [Theory]
    [InlineData(false, MaterialStatus.Draft)]
    [InlineData(true, MaterialStatus.Draft)]
    [InlineData(true, MaterialStatus.Published)]
    public async Task Participant_CanDownloadOtherParticipantsFile(bool asStudent, MaterialStatus status)
    {
        var (tutor, student, agreementId) = await CreateContextAsync();
        var now = DateTimeOffset.UtcNow;
        Guid? lessonId = asStudent
            ? await CreateLessonAsync(agreementId, now.AddDays(1), now.AddDays(1).AddHours(1))
            : null;
        var material = await CreateMaterialAsync(agreementId, asStudent ? tutor.AccessToken : student.AccessToken,
            status, lessonId);

        using var response = await DownloadAsync(material.Id, asStudent ? student.AccessToken : tutor.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ContentType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(FileName, response.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(FileBytes, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task OutsiderAndMissingMaterial_ReturnSameProblemCode()
    {
        var (tutor, _, agreementId) = await CreateContextAsync();
        var material = await CreateMaterialAsync(agreementId, tutor.AccessToken);
        var outsider = await CreateStudentAsync("outsider@test.pl", "outsider");

        using var inaccessible = await DownloadAsync(material.Id, outsider.AccessToken);
        using var missing = await DownloadAsync(Guid.NewGuid(), outsider.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, inaccessible.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemCodeAsync(inaccessible, "LearningMaterials.ContextNotFound");
        await AssertProblemCodeAsync(missing, "LearningMaterials.ContextNotFound");
    }

    [Theory]
    [InlineData(MaterialStatus.Archived)]
    [InlineData(MaterialStatus.Deleted)]
    public async Task UnavailableMaterial_ReturnsContextNotFound(MaterialStatus status)
    {
        var (tutor, student, agreementId) = await CreateContextAsync();
        var material = await CreateMaterialAsync(agreementId, tutor.AccessToken, status);

        using var response = await DownloadAsync(material.Id, student.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "LearningMaterials.ContextNotFound");
    }

    [Fact]
    public async Task MissingFile_ReturnsFileNotFound()
    {
        var (tutor, student, agreementId) = await CreateContextAsync();
        var material = await CreateMaterialAsync(agreementId, tutor.AccessToken);
        await Fixture.Services.GetRequiredService<IFileStorage>().DeleteAsync(material.Key, default);

        using var response = await DownloadAsync(material.Id, student.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "LearningMaterials.FileNotFound");
    }

    [Fact]
    public async Task UnauthenticatedCaller_Returns401()
    {
        using var response = await DownloadAsync(Guid.NewGuid(), null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorWithoutParticipantRole_Returns403()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var accountId = new UserAccountId(Guid.Parse(new JsonWebToken(tutor.AccessToken).Subject));
        var account = await ExecuteDbAsync(db => db.UserAccounts.AsNoTracking()
            .SingleAsync(user => user.Id == accountId));
        var administrator = new UserAccount(account.Email, account.PasswordHash, account.Profile, DateTimeOffset.UtcNow);
        administrator.AssignRole(UserRole.Administrator);
        using var scope = Fixture.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().Generate(administrator);

        using var response = await DownloadAsync(Guid.NewGuid(), token.Value);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(TestTutor Tutor, TestStudent Student, Guid AgreementId)> CreateContextAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreementId = await CreateAgreementAsync(tutor.Email, student.StudentId);
        return (tutor, student, agreementId);
    }

    private async Task<(Guid Id, string Key)> CreateMaterialAsync(Guid agreementId, string uploaderToken,
        MaterialStatus status = MaterialStatus.Draft, Guid? lessonId = null)
    {
        using var file = new MemoryStream(FileBytes);
        var storage = Fixture.Services.GetRequiredService<IFileStorage>();
        var key = await storage.SaveAsync(file, ".pdf", default);
        var uploaderId = new UserAccountId(Guid.Parse(new JsonWebToken(uploaderToken).Subject));

        var materialId = await ExecuteDbAsync(async db =>
        {
            var material = new LearningMaterial(new TutoringAgreementId(agreementId),
                lessonId is { } id ? new LessonId(id) : null, uploaderId, "Lesson notes",
                new FileDescriptor(FileName, ContentType, new FileSize(FileBytes.Length), new StorageLocation(key)),
                DateTimeOffset.UtcNow);
            db.LearningMaterials.Add(material);
            db.Entry(material).Property(entity => entity.Status).CurrentValue = status;
            await db.SaveChangesAsync();
            return material.Id.Value;
        });

        return (materialId, key);
    }

    private async Task<HttpResponseMessage> DownloadAsync(Guid materialId, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/learning-materials/{materialId}/download");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await Client.SendAsync(request);
    }
}
