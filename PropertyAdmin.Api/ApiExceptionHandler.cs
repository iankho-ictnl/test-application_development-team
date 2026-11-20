using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Logic;

namespace PropertyAdmin.Api;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    IStringLocalizer<BusinessRuleMessages> messages,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestCancelled(logger, httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (status, title, detail) = exception switch
        {
            BusinessRuleException e => (StatusCodes.Status400BadRequest, "Ongeldige aanvraag", Localize(httpContext, e.Message)),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Niet gevonden", null),
            _ => (StatusCodes.Status500InternalServerError, "Er is een onverwachte fout opgetreden", null),
        };
        if (status == StatusCodes.Status500InternalServerError)
            LogUnhandledException(logger, exception, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request {Path} was cancelled by the client.")]
    private static partial void LogRequestCancelled(ILogger logger, PathString path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Path}.")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, PathString path);

    /// <summary>
    /// The business-rule message in the request language (D-049). The handler runs outside the request
    /// localization middleware, so the culture comes from the request's culture feature, not from the
    /// current thread. A message without a resource entry is returned unchanged.
    /// </summary>
    private string Localize(HttpContext context, string message)
    {
        var culture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture;
        if (culture is null) return messages[message].Value;

        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            return messages[message].Value;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
