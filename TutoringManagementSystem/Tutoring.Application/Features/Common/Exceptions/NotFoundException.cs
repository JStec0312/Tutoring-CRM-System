using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Common.Exceptions;


public abstract class NotFoundException : UseCaseException
{
    protected NotFoundException(
        string code,
        string message)
        : base(code, message)
    {
    }
}