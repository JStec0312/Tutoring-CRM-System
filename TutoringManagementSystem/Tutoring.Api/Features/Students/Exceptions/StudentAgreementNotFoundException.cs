using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Students.Exceptions;

public sealed class StudentAgreementNotFoundException()
    : NotFoundException("Students.AgreementNotFound", "The tutoring agreement was not found or is not accessible to you.");
