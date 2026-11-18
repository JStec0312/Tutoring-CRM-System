using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.CompleteGoogleAccountLink;

public sealed class GoogleAccountLinkConflictException()
    : ConflictException("Auth.GoogleAccountLinkConflict", "The Google identity or CRM account already has a link.");
