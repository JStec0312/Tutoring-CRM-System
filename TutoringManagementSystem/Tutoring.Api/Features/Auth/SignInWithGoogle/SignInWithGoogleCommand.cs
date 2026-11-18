using MediatR;
using Tutoring.Api.Features.Common.Http;

namespace Tutoring.Api.Features.Auth.SignInWithGoogle;

public sealed record SignInWithGoogleCommand(string IdToken, RequestMetadata Metadata)
    : IRequest<SignInWithGoogleResult>;
