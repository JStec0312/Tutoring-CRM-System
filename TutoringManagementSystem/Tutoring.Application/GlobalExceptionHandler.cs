
using Tutoring.Domain.Common.Exceptions;

namespace Tutoring.Api;
using Features.Common.Exceptions;


using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var error = exception switch
        {
            NotFoundException notFoundException => new ErrorDetails(
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFoundException.Code,
                notFoundException.Message),
                
            

            ConflictException conflictException => new ErrorDetails(
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Code,
                conflictException.Message),

            ForbiddenException forbiddenException => new ErrorDetails(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenException.Code,
                forbiddenException.Message),

            AuthException authException => new ErrorDetails(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                authException.Code,
                authException.Message),

            DomainException domainException => new ErrorDetails(
                StatusCodes.Status422UnprocessableEntity,
                "Business rule violation",
                domainException.Code,
                domainException.Message),

            UseCaseException useCaseException => new ErrorDetails(
                StatusCodes.Status400BadRequest,
                "Request could not be completed",
                useCaseException.Code,
                useCaseException.Message),
            

            _ => new ErrorDetails(
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "Server.UnexpectedError",
                "An unexpected error occurred.")
        };

        if (error.StatusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "An unexpected exception occurred.");
        }

        httpContext.Response.StatusCode = error.StatusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = error.StatusCode,
                    Title = error.Title,
                    Detail = error.Detail,
                    Extensions =
                    {
                        ["code"] = error.Code
                    }
                }
            });
    }

    private sealed record ErrorDetails(
        int StatusCode,
        string Title,
        string Code,
        string Detail);
}