namespace Tutoring.Api.Features.Common.Http;

public sealed record RequestMetadata(
    string? IpAddress,
    string? UserAgent,
    string? TraceId);
