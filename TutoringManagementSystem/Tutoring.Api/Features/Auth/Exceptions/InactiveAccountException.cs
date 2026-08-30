using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class InactiveAccountException : ForbiddenException
{
    public InactiveAccountException() : base("Auth.InactiveAccount.", "Inactive account.")
    {
        
    }
}