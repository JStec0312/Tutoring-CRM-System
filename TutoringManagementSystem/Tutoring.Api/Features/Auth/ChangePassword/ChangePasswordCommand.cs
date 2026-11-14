using MediatR;
using Tutoring.Domain.Identity;

namespace Tutoring.Api.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(UserAccountId AccountId, string CurrentPassword, string NewPassword) : IRequest;
