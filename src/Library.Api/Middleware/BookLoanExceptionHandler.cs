using Library.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Library.Api.Middleware;

public sealed class BookLoanExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not LibraryDomainException domain)
        {
            return false;
        }

        httpContext.Response.StatusCode = domain.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = domain.StatusCode,
                Title = domain.Title,
                Detail = domain.Message,
            },
        });
    }
}