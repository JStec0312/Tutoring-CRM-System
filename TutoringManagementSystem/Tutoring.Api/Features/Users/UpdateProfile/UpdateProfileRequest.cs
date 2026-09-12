namespace Tutoring.Api.Features.Users.UpdateProfile;



public sealed record UpdateProfileRequest(
    string? UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);