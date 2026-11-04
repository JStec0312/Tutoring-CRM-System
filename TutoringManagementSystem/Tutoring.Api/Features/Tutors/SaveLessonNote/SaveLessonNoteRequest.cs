namespace Tutoring.Api.Features.Tutors.SaveLessonNote;

public sealed record SaveLessonNoteRequest(string? Topic, string? CoveredTopics, string? StudentProgress);
