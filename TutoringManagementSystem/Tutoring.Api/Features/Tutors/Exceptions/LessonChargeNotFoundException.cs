using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Tutors.Exceptions;

public sealed class LessonChargeNotFoundException(Guid tutorId, Guid lessonChargeId)
    : NotFoundException("Billing.LessonChargeNotFound", $"Lesson charge {lessonChargeId} for tutor {tutorId} was not found.");
