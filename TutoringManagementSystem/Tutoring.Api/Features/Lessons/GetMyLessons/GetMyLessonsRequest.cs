using System.ComponentModel.DataAnnotations;

namespace Tutoring.Api.Features.Lessons.GetMyLessons;

public sealed class GetMyLessonsRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 4;

    [RegularExpression("^(all|upcoming|past)$")]
    public string View { get; init; } = "all";

    public bool TutorOnly { get; init; }
    public Guid? AgreementId { get; init; }
}
