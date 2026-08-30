namespace Tutoring.Api.Features.Common.Exceptions;

/// <summary>
/// Base exception for cases where the caller is authenticated but not allowed
/// to perform the requested action (e.g. inactive account, unconfirmed email,
/// lack of permissions on a resource). Maps to 403 Forbidden.
/// </summary>
public abstract class ForbiddenException : UseCaseException
{
    protected ForbiddenException(
        string code,
        string message)
        : base(code, message)
    {
    }
}
