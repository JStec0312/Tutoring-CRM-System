using MediatR;

namespace Tutoring.Api.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
