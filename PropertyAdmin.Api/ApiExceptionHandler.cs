using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Logic;

namespace PropertyAdmin.Api;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    IStringLocalizer<BusinessRuleMessages> messages,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
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
            BusinessRuleException e => (StatusCodes.Status400BadRequest, "Ongeldige aanvraag", Localize(context, e.Message)),
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
