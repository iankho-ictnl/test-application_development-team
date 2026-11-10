using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PropertyAdmin.Logic;

namespace PropertyAdmin.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Path} was cancelled by the client.", context.Request.Path);
            context.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (status, title, detail) = exception switch
        {
            BusinessRuleException e => (StatusCodes.Status400BadRequest, "Ongeldige aanvraag", e.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Niet gevonden", null),
            _ => (StatusCodes.Status500InternalServerError, "Er is een onverwachte fout opgetreden", null),
        };
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Path}.", context.Request.Path);

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }
}
