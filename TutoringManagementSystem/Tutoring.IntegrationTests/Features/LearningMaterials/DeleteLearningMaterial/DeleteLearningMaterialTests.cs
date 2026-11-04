using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Messaging.Outbox;
using Tutoring.Infrastructure.Persistence;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.LearningMaterials.DeleteLearningMaterial;

[Collection(IntegrationTestCollection.Name)]
public sealed class DeleteLearningMaterialTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AuthorizedParticipant_MarksMaterialDeletedAndEnqueuesOneCleanup(bool asStudent, bool uploadedByStudent)
    {
        var (tutor, student, agreementId) = await CreateContextAsync();
        var (materialId, storageKey) = await CreateMaterialAsync(
            agreementId, uploadedByStudent ? student.AccessToken : tutor.AccessToken);
        var token = asStudent ? student.AccessToken : tutor.AccessToken;

        var responses = await Task.WhenAll(DeleteAsync(materialId, token), DeleteAsync(materialId, token));
        foreach (var response in responses)
        {
            using (response)
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        await ExecuteDbAsync(async db =>
        {
            var material = await db.LearningMaterials.SingleAsync();
            Assert.Equal(MaterialStatus.Deleted, material.Status);
            var message = await db.OutboxMessages.SingleAsync(x => x.Type == LearningMaterialCleanup.MessageType);
            Assert.Equal(materialId.ToString("D"), message.Payload);
            Assert.Null(message.ProcessedAtUtc);
        });
        Assert.True(File.Exists(Path.Combine(Fixture.StorageRoot, storageKey)));

        using var download = await SendAsync(
            HttpMethod.Get, $"api/learning-materials/{materialId}/download", token);
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
        await AssertProblemCodeAsync(download, "LearningMaterials.ContextNotFound");
    }

    [Fact]
    public async Task InaccessibleAndMissingMaterials_ReturnSameProblemCode()
    {
        var (tutor, student, agreementId) = await CreateContextAsync();
        var (materialId, storageKey) = await CreateMaterialAsync(agreementId, tutor.AccessToken);
        var otherTutor = await CreateTutorAsync("other-tutor@test.pl", "other-tutor");
        var otherStudent = await CreateStudentAsync("other-student@test.pl", "other-student");

        using var studentResponse = await DeleteAsync(materialId, student.AccessToken);
        using var tutorResponse = await DeleteAsync(materialId, otherTutor.AccessToken);
        using var outsiderResponse = await DeleteAsync(materialId, otherStudent.AccessToken);
        using var missingResponse = await DeleteAsync(Guid.NewGuid(), tutor.AccessToken);

        foreach (var response in new[] { studentResponse, tutorResponse, outsiderResponse, missingResponse })
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await AssertProblemCodeAsync(response, "LearningMaterials.ContextNotFound");
        }

        await ExecuteDbAsync(async db =>
        {
            Assert.Equal(MaterialStatus.Draft, (await db.LearningMaterials.SingleAsync()).Status);
            Assert.False(await db.OutboxMessages.AnyAsync(x => x.Type == LearningMaterialCleanup.MessageType));
        });
        Assert.True(File.Exists(Path.Combine(Fixture.StorageRoot, storageKey)));
    }

    [Fact]
    public async Task UnauthenticatedCaller_Returns401()
    {
        using var response = await DeleteAsync(Guid.NewGuid(), null);
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

        using var response = await DeleteAsync(Guid.NewGuid(), token.Value);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cleanup_RetriesStorageFailureAndRemovesMetadata(bool fileAlreadyMissing)
    {
        var (tutor, _, agreementId) = await CreateContextAsync();
        var (materialId, storageKey) = await CreateMaterialAsync(agreementId, tutor.AccessToken);
        using var response = await DeleteAsync(materialId, tutor.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var storage = Fixture.Services.GetRequiredService<IFileStorage>();
        if (fileAlreadyMissing)
            await storage.DeleteAsync(storageKey, CancellationToken.None);

        using var scope = Fixture.Services.CreateScope();
        var services = new ServiceCollection();
        services.AddSingleton(scope.ServiceProvider.GetRequiredService<DbContextOptions<TutoringDbContext>>());
        services.AddScoped<TutoringDbContext>();
        services.AddSingleton<IFileStorage>(new FailFirstDeleteStorage(storage));
        await using var provider = services.BuildServiceProvider();
        using var worker = new OutboxProcessor(provider.GetRequiredService<IServiceScopeFactory>(),
            new NoOpPublisher(), TimeProvider.System, NullLogger<OutboxProcessor>.Instance,
            Options.Create(new OutboxOptions { ProcessingIntervalSeconds = 1 }));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await worker.StartAsync(timeout.Token);
        try
        {
            while (!await ExecuteDbAsync(db => db.OutboxMessages.AnyAsync(
                       x => x.Type == LearningMaterialCleanup.MessageType && x.ProcessedAtUtc != null, timeout.Token)))
                await Task.Delay(100, timeout.Token);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        await ExecuteDbAsync(async db =>
        {
            Assert.False(await db.LearningMaterials.AnyAsync());
            var message = await db.OutboxMessages.SingleAsync(x => x.Type == LearningMaterialCleanup.MessageType);
            Assert.Equal(1, message.RetryCount);
            Assert.Null(message.Error);
        });
        Assert.False(File.Exists(Path.Combine(Fixture.StorageRoot, storageKey)));
        using var repeatedDelete = await DeleteAsync(materialId, tutor.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, repeatedDelete.StatusCode);
    }

    private async Task<(TestTutor Tutor, TestStudent Student, Guid AgreementId)> CreateContextAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        return (tutor, student, await CreateAgreementAsync(tutor.Email, student.StudentId));
    }

    private async Task<(Guid Id, string StorageKey)> CreateMaterialAsync(Guid agreementId, string uploaderToken)
    {
        using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var storage = Fixture.Services.GetRequiredService<IFileStorage>();
        var storageKey = await storage.SaveAsync(stream, ".pdf", CancellationToken.None);
        var uploaderId = new UserAccountId(Guid.Parse(new JsonWebToken(uploaderToken).Subject));
        var materialId = await ExecuteDbAsync(async db =>
        {
            var material = new LearningMaterial(new TutoringAgreementId(agreementId), null, uploaderId, "Notes",
                new FileDescriptor("notes.pdf", "application/pdf", new FileSize(stream.Length),
                    new StorageLocation(storageKey)), DateTimeOffset.UtcNow);
            db.LearningMaterials.Add(material);
            await db.SaveChangesAsync();
            return material.Id.Value;
        });
        return (materialId, storageKey);
    }

    private Task<HttpResponseMessage> DeleteAsync(Guid materialId, string? token) =>
        SendAsync(HttpMethod.Delete, $"api/learning-materials/{materialId}", token);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await Client.SendAsync(request, CancellationToken.None);
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task PublishAsync(Guid messageId, string type, string payload, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FailFirstDeleteStorage(IFileStorage storage) : IFileStorage
    {
        private bool _failed;

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            if (!_failed)
            {
                _failed = true;
                throw new IOException("Simulated storage failure.");
            }

            return storage.DeleteAsync(storageKey, cancellationToken);
        }

        public Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken cancellationToken) =>
            storage.SaveAsync(content, fileExtension, cancellationToken);

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            storage.OpenReadAsync(storageKey, cancellationToken);
    }
}
