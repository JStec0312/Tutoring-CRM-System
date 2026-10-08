using MediatR;
using Tutoring.Api.Features.Common.Http;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Users.ChangePassword;

public sealed record ChangePasswordCommand(
    UserAccountId UserAccountId,
    string CurrentPassword,
    string NewPassword,
    RequestMetadata RequestMetadata)
    : IRequest;
