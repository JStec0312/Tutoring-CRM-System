namespace Tutoring.Domain.Common.Exceptions;

public abstract class UseCaseException : Exception
{
    protected UseCaseException(
        string code,
        string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
