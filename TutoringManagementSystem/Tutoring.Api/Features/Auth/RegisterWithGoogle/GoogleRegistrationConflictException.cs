using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.RegisterWithGoogle;

public sealed class GoogleRegistrationConflictException()
    : ConflictException("Auth.GoogleRegistrationConflict", "An account already exists for this email or Google identity.");
