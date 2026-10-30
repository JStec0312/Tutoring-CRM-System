using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tutoring.Domain.Identity;
using Tutoring.Domain.LearningMaterials;
using Tutoring.Domain.TutoringAgreements;
using Tutoring.Infrastructure.Authentication;
using Tutoring.IntegrationTests.Infrastructure;

namespace Tutoring.IntegrationTests.Features.LearningMaterials.AttachLearningMaterialToLesson;

[Collection(IntegrationTestCollection.Name)]
public sealed class AttachLearningMaterialToLessonTests(IntegrationTestFixture fixture) : LessonIntegrationTestBase(fixture)
{
    protected override DateTimeOffset AgreementCreatedAtUtc => DateTimeOffset.UtcNow;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Participant_CanAttachRepeatAndReplace(bool asStudent)
    {
        var (tutor, student, agreement, material, first) = await ContextAsync();
        var now = DateTimeOffset.UtcNow;
        var second = await CreateLessonAsync(agreement, now.AddDays(2), now.AddDays(2).AddHours(1));
        var token = asStudent ? student.AccessToken : tutor.AccessToken;
        foreach (var lesson in new[] { first, first, second })
        {
            using var response = await AttachAsync(token, material, lesson);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(lesson, await StoredLessonAsync(material));
        }
    }

    [Theory]
    [InlineData("outsiderTutor")]
    [InlineData("outsiderStudent")]
    [InlineData("missingMaterial")]
    [InlineData("missingLesson")]
    [InlineData("otherAgreement")]
    public async Task InaccessibleContext_Returns404WithoutChangingMaterial(string scenario)
    {
        var (tutor, student, _, material, first) = await ContextAsync();
        using var initial = await AttachAsync(tutor.AccessToken, material, first);
        Assert.Equal(HttpStatusCode.NoContent, initial.StatusCode);
        var token = tutor.AccessToken;
        var targetMaterial = material;
        var targetLesson = first;
        if (scenario == "outsiderTutor") token = (await CreateTutorAsync("outsider@test.pl", "outsider")).AccessToken;
        if (scenario == "outsiderStudent") token = (await CreateStudentAsync("outsider@test.pl", "outsider")).AccessToken;
        if (scenario == "missingMaterial") targetMaterial = Guid.NewGuid();
        if (scenario == "missingLesson") targetLesson = Guid.NewGuid();
        if (scenario == "otherAgreement")
        {
            var other = await CreateAgreementAsync(tutor.Email, student.StudentId);
            var now = DateTimeOffset.UtcNow;
            targetLesson = await CreateLessonAsync(other, now.AddDays(2), now.AddDays(2).AddHours(1));
        }
        using var response = await AttachAsync(token, targetMaterial, targetLesson);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemCodeAsync(response, "LearningMaterials.ContextNotFound");
        Assert.Equal(first, await StoredLessonAsync(material));
    }

    [Theory]
    [InlineData("none", 401)]
    [InlineData("parent", 403)]
    [InlineData("invalid", 401)]
    [InlineData("missing", 401)]
    [InlineData("empty", 401)]
    public async Task AuthenticationFailure_DoesNotChangeMaterial(string kind, int status)
    {
        var (_, _, _, material, lesson) = await ContextAsync();
        using var response = await AttachAsync(kind == "none" ? null : Token(kind), material, lesson);
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Null(await StoredLessonAsync(material));
    }

    [Fact]
    public async Task InvalidInputs_DoNotChangeMaterial()
    {
        var (tutor, _, _, material, _) = await ContextAsync();
        using var empty = await AttachAsync(tutor.AccessToken, material, Guid.Empty);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
        await AssertProblemCodeAsync(empty, "LearningMaterials.InvalidMetadata");
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/learning-materials/{material}/lesson");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tutor.AccessToken);
        request.Content = JsonContent.Create(new { });
        using var missing = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Null(await StoredLessonAsync(material));
    }

    private async Task<(TestTutor Tutor, TestStudent Student, Guid Agreement, Guid Material, Guid Lesson)> ContextAsync()
    {
        var tutor = await CreateTutorAsync("tutor@test.pl", "tutor");
        var student = await CreateStudentAsync("student@test.pl", "student");
        var agreement = await CreateAgreementAsync(tutor.Email, student.StudentId);
        var now = DateTimeOffset.UtcNow;
        var lesson = await CreateLessonAsync(agreement, now.AddDays(1), now.AddDays(1).AddHours(1));
        var material = await ExecuteDbAsync(async db =>
        {
            var entity = new LearningMaterial(new TutoringAgreementId(agreement), null,
                new UserAccountId(Guid.Parse(new JsonWebToken(tutor.AccessToken).Subject)), "Notes",
                new FileDescriptor("notes.pdf", "application/pdf", new FileSize(10), new StorageLocation("key.pdf")), now);
            db.LearningMaterials.Add(entity);
            await db.SaveChangesAsync();
            return entity.Id.Value;
        });
        return (tutor, student, agreement, material, lesson);
    }

    private async Task<Guid?> StoredLessonAsync(Guid material)
    {
        var entity = await ExecuteDbAsync(db => db.LearningMaterials.AsNoTracking()
            .SingleAsync(m => m.Id == new LearningMaterialId(material)));
        return entity.LessonId?.Value;
    }

    private async Task<HttpResponseMessage> AttachAsync(string? token, Guid material, Guid lesson)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/learning-materials/{material}/lesson");
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { lessonId = lesson });
        return await Client.SendAsync(request);
    }

    private string Token(string kind)
    {
        var options = Fixture.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var claims = new List<Claim> { new("role", kind == "parent" ? "Parent" : "Tutor") };
        if (kind != "missing") claims.Add(new Claim("sub", kind switch { "invalid" => "invalid", "empty" => Guid.Empty.ToString(), _ => Guid.NewGuid().ToString() }));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims), Issuer = options.Issuer, Audience = options.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5), SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)), SecurityAlgorithms.HmacSha256)
        });
    }
}
