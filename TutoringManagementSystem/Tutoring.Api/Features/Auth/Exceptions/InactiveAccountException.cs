using Tutoring.Api.Features.Common.Exceptions;

namespace Tutoring.Api.Features.Auth.Exceptions;

public class InactiveAccountException : AuthException
{
    public InactiveAccountException() : base("Auth.InactiveAccount.", "Inactive account.")
    {
        
    }
}