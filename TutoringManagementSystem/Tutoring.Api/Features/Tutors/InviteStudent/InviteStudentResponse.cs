namespace Tutoring.Api.Features.Tutors.InviteStudent;

public sealed record InviteStudentResponse(
    Guid InvitationId,
    string InvitationUrl,
    DateTimeOffset ValidUntilUtc);