using System.Text.Json.Serialization;
using Tutoring.Domain.Lessons;

namespace Tutoring.Api.Features.Tutors.SetLessonStatus;

public sealed record SetLessonStatusRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    LessonStatus Status);