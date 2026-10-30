using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Api.Features.LearningMaterials.Exceptions;
using Tutoring.Api.Features.LearningMaterials.UploadLearningMaterial;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Infrastructure.Authentication;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Persistence;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.LearningMaterials.UploadLearningMaterial;

[Collection(IntegrationTestCollection.Name)]
public sealed class UploadLearningMaterialTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow;
    private const string Endpoint = "/api/learning-materials";
    private static byte[] Pdf => "%PDF-1.7\nTest material"u8.ToArray();

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Upload_AsParticipant_ShouldPersistFileAndCorrectUploader(bool asStudent, bool withLesson)
    {
        var (tutor, student, agreement) = await ContextAsync();
        var now = DateTimeOffset.UtcNow;
        Guid? lesson = withLesson ? await CreateLessonAsync(agreement, now.AddDays(1), now.AddDays(1).AddHours(1)) : null;
        var token = asStudent ? student.AccessToken : tutor.AccessToken;
        var before = DateTimeOffset.UtcNow;
        using var response = await UploadAsync(token, agreement, lesson);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<UploadLearningMaterialResponse>())!;
        Assert.Equal(agreement, result.TutoringAgreementId);
        Assert.Equal(lesson, result.LessonId);
        Assert.Equal("Draft", result.Status);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(Pdf.Length, result.SizeBytes);
        Assert.Equal(Subject(token), result.UploadedByUserAccountId);
        Assert.InRange(result.CreatedAtUtc, before, DateTimeOffset.UtcNow);
        var material = await ExecuteDbAsync(db => db.LearningMaterials.AsNoTracking()
            .SingleAsync(m => m.Id == new LearningMaterialId(result.MaterialId)));
        Assert.Equal(result.UploadedByUserAccountId, material.UploadedByUserAccountId.Value);
        Assert.Equal(agreement, material.TutoringAgreementId.Value);
        Assert.Equal(lesson, material.LessonId?.Value);
        Assert.Equal(MaterialStatus.Draft, material.Status);
        Assert.Equal("Notes", material.Title);
        Assert.Equal(Pdf, await File.ReadAllBytesAsync(Path.Combine(Fixture.StorageRoot, material.File.StorageLocation.Value)));
        Assert.False(await ExecuteDbAsync(db => db.MaterialAssignments.AnyAsync()));
        Assert.DoesNotContain("storageLocation", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("notes.JPG", "image/jpeg")]
    [InlineData("notes.png", "image/png")]
    public async Task Upload_Image_ShouldUseDetectedMime(string name, string expectedType)
    {
        var (tutor, _, agreement) = await ContextAsync();
        byte[] bytes = expectedType == "image/jpeg" ? [255, 216, 255, 224] : [137, 80, 78, 71, 13, 10, 26, 10];
        using var response = await UploadAsync(tutor.AccessToken, agreement, bytes: bytes, fileName: name);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(expectedType, (await response.Content.ReadFromJsonAsync<UploadLearningMaterialResponse>())!.ContentType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_AsUnrelatedParticipant_ShouldReturn404WithoutWrites(bool asStudent)
    {
        var (_, _, agreement) = await ContextAsync();
        var token = asStudent
            ? (await CreateStudentAsync("outsider-student@test.pl", "outsider")).AccessToken
            : (await CreateTutorAsync("outsider-tutor@test.pl", "outsider")).AccessToken;
        using var response = await UploadAsync(token, agreement);
        await AssertRejectedAsync(response, HttpStatusCode.NotFound, "LearningMaterials.ContextNotFound");
        Assert.Equal("The tutoring agreement was not found or is not accessible to you.",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Upload_WithMissingAgreement_ShouldReturn404()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        using var response = await UploadAsync(tutor.AccessToken, Guid.NewGuid());
        await AssertRejectedAsync(response, HttpStatusCode.NotFound, "LearningMaterials.ContextNotFound");
        Assert.Equal("The tutoring agreement was not found or is not accessible to you.",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_WithMissingOrDifferentAgreementLesson_ShouldReturn404(bool missing)
    {
        var (tutor, student, agreement) = await ContextAsync();
        var otherAgreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var now = DateTimeOffset.UtcNow;
        var lesson = missing ? Guid.NewGuid() : await CreateLessonAsync(otherAgreement, now.AddDays(1), now.AddDays(1).AddHours(1));
        using var response = await UploadAsync(student.AccessToken, agreement, lesson);
        await AssertRejectedAsync(response, HttpStatusCode.NotFound, "LearningMaterials.ContextNotFound");
        Assert.Equal("The lesson was not found in the specified tutoring agreement.",
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("empty", "LearningMaterials.EmptyFile", 400)]
    [InlineData("format", "LearningMaterials.UnsupportedFormat", 400)]
    [InlineData("signature", "LearningMaterials.InvalidFileContent", 400)]
    [InlineData("size", "LearningMaterials.FileTooLarge", 413)]
    [InlineData("title", "LearningMaterials.InvalidMetadata", 422)]
    public async Task Upload_WithInvalidInput_ShouldRejectWithoutWrites(string invalid, string code, int status)
    {
        var (tutor, _, agreement) = await ContextAsync();
        var bytes = invalid switch
        {
            "empty" => [],
            "signature" => "not a pdf"u8.ToArray(),
            "size" => new byte[20_000_001],
            _ => Pdf
        };
        using var response = await UploadAsync(tutor.AccessToken, agreement, bytes: bytes,
            fileName: invalid == "format" ? "notes.exe" : "notes.pdf",
            title: invalid == "title" ? new string('a', 201) : "Notes");
        await AssertRejectedAsync(response, (HttpStatusCode)status, code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_WithMissingRequiredField_ShouldReturn400(bool omitFile)
    {
        var (tutor, _, agreement) = await ContextAsync();
        using var request = CreateAuthorizedRequest(HttpMethod.Post, Endpoint, tutor.AccessToken);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Notes"), "title");
        if (omitFile) form.Add(new StringContent(agreement.ToString()), "tutoringAgreementId");
        else form.Add(new ByteArrayContent(Pdf), "file", "notes.pdf");
        request.Content = form;
        using var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoWritesAsync();
    }

    [Fact]
    public async Task Upload_WithoutAuthentication_ShouldReturn401()
    {
        using var response = await UploadAsync(null, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertNoWritesAsync();
    }

    [Theory]
    [InlineData("Parent", "valid", 403)]
    [InlineData("Tutor", "invalid", 401)]
    [InlineData("Student", "missing", 401)]
    public async Task Upload_WithInvalidRoleOrSubject_ShouldReject(string role, string subject, int status)
    {
        var token = Token(role, subject);
        using var response = await UploadAsync(token, Guid.NewGuid());
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        await AssertNoWritesAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handler_WhenPersistenceFailsOrRequestIsCancelled_ShouldDeleteSavedFile(bool cancel)
    {
        var (tutor, _, agreement) = await ContextAsync();
        using var scope = Fixture.Services.CreateScope();
        var existing = scope.ServiceProvider.GetRequiredService<TutoringDbContext>();
        var builder = new DbContextOptionsBuilder<TutoringDbContext>()
            .UseSqlServer(existing.Database.GetConnectionString());
        if (!cancel) builder.AddInterceptors(new FailingSaveInterceptor());
        await using var db = new TutoringDbContext(builder.Options);
        var realStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        using var cts = new CancellationTokenSource();
        var storage = new CancellingStorage(realStorage, cancel ? cts : null);
        var handler = new UploadLearningMaterialHandler(db,
            scope.ServiceProvider.GetRequiredService<IUploadedFileValidator>(), storage,
            TimeProvider.System, NullLogger<UploadLearningMaterialHandler>.Instance);
        using var content = new MemoryStream(Pdf);
        var command = new UploadLearningMaterialCommand(new UserAccountId(Subject(tutor.AccessToken)), agreement,
            null, "Notes", content, "notes.pdf", Pdf.Length, new RequestMetadata(null, null, "failure-test"));
        if (cancel)
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.Handle(command, cts.Token));
        else
        {
            var error = await Assert.ThrowsAsync<MaterialUploadFailedException>(() => handler.Handle(command, default));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }
        Assert.True(storage.Deleted);
        await AssertNoWritesAsync();
    }

    private async Task<(TestTutor Tutor, TestStudent Student, Guid Agreement)> ContextAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        return (tutor, student, await CreateAgreementAsync(tutor.Email, student.StudentId));
    }

    private async Task<HttpResponseMessage> UploadAsync(string? token, Guid agreement,
        Guid? lesson = null, byte[]? bytes = null, string fileName = "notes.pdf", string title = "Notes")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(agreement.ToString()), "tutoringAgreementId");
        if (lesson is not null) form.Add(new StringContent(lesson.ToString()!), "lessonId");
        var file = new ByteArrayContent(bytes ?? Pdf);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        request.Content = form;
        return await Client.SendAsync(request);
    }

    private async Task AssertRejectedAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        await AssertProblemCodeAsync(response, code);
        await AssertNoWritesAsync();
    }

    private async Task AssertNoWritesAsync()
    {
        Assert.False(await ExecuteDbAsync(db => db.LearningMaterials.AnyAsync()));
        Assert.False(Directory.Exists(Fixture.StorageRoot) && Directory.EnumerateFiles(Fixture.StorageRoot).Any());
    }

    private static Guid Subject(string token) => Guid.Parse(new JsonWebToken(token).Subject);

    private string Token(string role, string subject)
    {
        var options = Fixture.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var claims = new List<Claim> { new("role", role) };
        if (subject != "missing") claims.Add(new Claim("sub", subject == "valid" ? Guid.NewGuid().ToString() : subject));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims), Issuer = options.Issuer, Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)), SecurityAlgorithms.HmacSha256)
        });
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated metadata persistence failure.");
    }

    private sealed class CancellingStorage(IFileStorage inner, CancellationTokenSource? cancellation) : IFileStorage
    {
        public bool Deleted { get; private set; }
        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken token)
        {
            var key = await inner.SaveAsync(content, extension, token);
            cancellation?.Cancel();
            return key;
        }
        public Task<Stream> OpenReadAsync(string key, CancellationToken token) => inner.OpenReadAsync(key, token);
        public async Task DeleteAsync(string key, CancellationToken token)
        {
            Assert.False(token.IsCancellationRequested);
            await inner.DeleteAsync(key, token);
            Deleted = true;
        }
    }
}
