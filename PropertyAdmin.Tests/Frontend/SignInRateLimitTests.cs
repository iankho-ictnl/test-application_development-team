extern alias Frontend;

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Pages.Account;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-001, security finding S-17, D-044: at most 10 sign-in attempts per minute per client IP
/// address, for the password step and the two-factor step each; the 11th is refused with a
/// localized page and status 429, without calling the API.
/// </summary>
public sealed class SignInRateLimitTests
{
    private const string TooMany = "Too many sign-in attempts. Wait a minute and try again.";
    private static readonly IPAddress ClientA = IPAddress.Parse("203.0.113.10");
    private static readonly IPAddress ClientB = IPAddress.Parse("203.0.113.20");

    [Fact]
    public async Task The_eleventh_password_attempt_in_a_minute_is_refused_with_429_without_calling_the_api()
    {
        using var limiter = new SignInRateLimiter(10, TimeSpan.FromMinutes(1));
        var api = new StubApi();

        for (var i = 0; i < 10; i++)
        {
            var allowed = CreateLogin(api, limiter, ClientA);
            await allowed.OnPostAsync();
            Assert.NotEqual(StatusCodes.Status429TooManyRequests, allowed.HttpContext.Response.StatusCode);
        }
        Assert.Equal(10, api.Calls);

        var refused = CreateLogin(api, limiter, ClientA);
        var result = await refused.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, refused.HttpContext.Response.StatusCode);
        Assert.Contains(TooMany, refused.ModelState[""]!.Errors.Select(e => e.ErrorMessage));
        Assert.Equal(10, api.Calls);
    }

    [Fact]
    public async Task Another_client_ip_keeps_its_own_attempts()
    {
        using var limiter = new SignInRateLimiter(10, TimeSpan.FromMinutes(1));
        var api = new StubApi();
        for (var i = 0; i < 11; i++)
        {
            await CreateLogin(api, limiter, ClientA).OnPostAsync();
        }

        var other = CreateLogin(api, limiter, ClientB);
        await other.OnPostAsync();

        Assert.NotEqual(StatusCodes.Status429TooManyRequests, other.HttpContext.Response.StatusCode);
        Assert.Equal(11, api.Calls);
    }

    [Fact]
    public async Task The_eleventh_two_factor_attempt_is_refused_and_counts_apart_from_the_password_step()
    {
        using var limiter = new SignInRateLimiter(10, TimeSpan.FromMinutes(1));
        var api = new StubApi();
        for (var i = 0; i < 11; i++)
        {
            await CreateLogin(api, limiter, ClientA).OnPostAsync();
        }
        Assert.Equal(10, api.Calls);

        for (var i = 0; i < 10; i++)
        {
            var allowed = CreateTwoFactor(api, limiter, ClientA);
            await allowed.OnPostAsync();
            Assert.NotEqual(StatusCodes.Status429TooManyRequests, allowed.HttpContext.Response.StatusCode);
        }
        Assert.Equal(20, api.Calls);

        var refused = CreateTwoFactor(api, limiter, ClientA);
        var result = await refused.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, refused.HttpContext.Response.StatusCode);
        Assert.Contains(TooMany, refused.ModelState[""]!.Errors.Select(e => e.ErrorMessage));
        Assert.Equal(20, api.Calls);
    }

    [Fact]
    public void The_limit_and_window_must_be_positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignInRateLimiter(0, TimeSpan.FromMinutes(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SignInRateLimiter(10, TimeSpan.Zero));
    }

    private static LoginModel CreateLogin(StubApi api, SignInRateLimiter limiter, IPAddress ip)
    {
        var (client, signIn, httpContext) = Setup(api, ip);
        return new LoginModel(client, signIn, limiter, new EchoLocalizer())
        {
            PageContext = PageContextFor(httpContext),
            UserName = "user",
            Password = "wrong",
        };
    }

    private static LoginTwoFactorModel CreateTwoFactor(StubApi api, SignInRateLimiter limiter, IPAddress ip)
    {
        var (client, signIn, httpContext) = Setup(api, ip);
        return new LoginTwoFactorModel(client, signIn, limiter, new EchoLocalizer())
        {
            PageContext = PageContextFor(httpContext),
            TempData = new TempDataDictionary(httpContext, new NoTempDataProvider()),
            Challenge = "challenge",
            Code = "000000",
        };
    }

    private static (ApiClient, SignInService, DefaultHttpContext) Setup(StubApi api, IPAddress ip)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = ip;
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var client = new ApiClient(new HttpClient(api, disposeHandler: false) { BaseAddress = new Uri("http://api.test/") });
        return (client, new SignInService(accessor), httpContext);
    }

    private static PageContext PageContextFor(HttpContext httpContext) => new()
    {
        HttpContext = httpContext,
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
    };

    /// <summary>Stands in for the API: every sign-in attempt is refused, and each call is counted.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }

    private sealed class EchoLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(System.Globalization.CultureInfo.CurrentCulture, name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
