using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.ConfirmEmail;


public sealed record ConfirmEmailCommand(
    string Token, RequestMetadata requestMetadata) : IRequest;