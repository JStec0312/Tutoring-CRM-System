namespace Tutoring.Domain.Common.Exceptions;

public class EmptyFieldException : Exception
{
    public EmptyFieldException(string fieldName)
        : base($"Field '{fieldName}' cannot be empty.")
    {
    }
    
}