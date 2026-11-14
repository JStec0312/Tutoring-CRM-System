using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.ChangePassword;

internal sealed class IncorrectCurrentPasswordException()
    : UseCaseException("Auth.IncorrectCurrentPassword", "The current password is incorrect.");
