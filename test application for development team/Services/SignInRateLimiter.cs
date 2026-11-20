using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace test_application_for_development_team.Services;

/// <summary>
/// Security finding S-17 (REQ-001, D-044): limits the sign-in attempts per client IP address,
/// separately for the password step and the two-factor step. The pages ask this limiter before
/// they call the API, so a refused attempt is a localized page with status 429.
/// The client IP address comes from the forwarded headers of the known proxy (Caddy).
/// </summary>
public sealed class SignInRateLimiter : IDisposable
{
    public const string PasswordStep = "login";
    public const string TwoFactorStep = "login-2fa";

    private readonly PartitionedRateLimiter<string> _limiter;

    public SignInRateLimiter(int permitLimit, TimeSpan window)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(permitLimit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        _limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    /// <summary>True when this attempt is allowed; false when the client made too many attempts in the window.</summary>
    public bool TryAcquire(string step, HttpContext context)
    {
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        using var lease = _limiter.AttemptAcquire($"{step}|{client}");
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
