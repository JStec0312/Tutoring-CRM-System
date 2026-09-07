namespace Tutoring.Api.Features.Common.Http;

public sealed record RequestMetadata(
    string? IpAddress,
    string? UserAgent,
    string? TraceId)
{
    public override string ToString() =>
        $"IpAddress: {IpAddress}, UserAgent: {UserAgent}, TraceId: {TraceId}";

}