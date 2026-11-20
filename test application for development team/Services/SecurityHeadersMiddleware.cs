namespace test_application_for_development_team.Services;

/// <summary>
/// Security finding S-25 (REQ-001, REQ-025): every response carries a content security policy and the
/// other browser security headers. The policy allows no inline script, so all page script lives in
/// wwwroot/js (site.js and the libraries).
/// Security finding S-33: the headers are set when the response starts (Response.OnStarting), not
/// when the request comes in, because the exception handler clears the response headers before it
/// renders the 500 error page; OnStarting callbacks survive that, so error pages get them too.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpResponse)state).Headers;
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers.XFrameOptions = "DENY";
            return Task.CompletedTask;
        }, context.Response);
        return next(context);
    }
}
