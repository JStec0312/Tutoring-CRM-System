using Tutoring.Api.Features.Common.Exceptions;
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.LinkGoogleAccount;

internal sealed class InvalidGoogleLinkCredentialException()
    : UseCaseException("Auth.InvalidGoogleCredential",
        "The Google credential or linking attempt is invalid.");

internal sealed class GoogleAccountLinkConflictException()
    : ConflictException("Auth.GoogleLinkConflict",
        "The CRM account or Google account is already linked.");
