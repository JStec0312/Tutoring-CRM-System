
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api.Features.Common.Exceptions;

public abstract class ConflictException : UseCaseException
{
    protected ConflictException(
        string code,
        string message)
        : base(code, message)
    {
    }
}