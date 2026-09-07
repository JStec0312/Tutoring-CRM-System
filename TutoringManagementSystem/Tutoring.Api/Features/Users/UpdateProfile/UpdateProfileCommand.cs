namespace Tutoring.Api.Features.Users.UpdateProfile;

using MediatR;
using Domain.Identity;
using Common.Http;

public sealed record UpdateProfileCommand(
    UserAccountId UserAccountId,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    RequestMetadata RequestMetadata)
    : IRequest;