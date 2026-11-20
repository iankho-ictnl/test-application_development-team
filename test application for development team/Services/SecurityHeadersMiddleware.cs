namespace test_application_for_development_team.Services;

/// <summary>
/// Security finding S-25 (REQ-001, REQ-025): every response carries a content security policy and the
/// other browser security headers. The policy allows no inline script, so all page script lives in
/// wwwroot/js (site.js and the libraries).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers.XFrameOptions = "DENY";
        return next(context);
    }
}
