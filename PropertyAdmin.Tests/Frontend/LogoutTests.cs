extern alias Frontend;

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Frontend::test_application_for_development_team.Pages.Account;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-001, security finding S-26 (UI), D-036: sign-out first ends the session at the API
/// (POST api/auth/logout with the user's token), and always removes the cookie.
/// </summary>
public sealed class LogoutTests
{
    [Fact]
    public async Task Sign_out_calls_the_api_logout_with_the_token_before_the_cookie_is_removed()
    {
        var auth = new RecordingAuthentication();
        var api = new StubApi(auth, _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var page = CreatePage(api, auth);

        var result = await page.OnPostAsync();

        var request = Assert.Single(api.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/auth/logout", request.Path);
        Assert.Equal("Bearer test-token", request.Authorization);
        Assert.False(request.SignedOutBefore);
        Assert.True(auth.SignedOut);
        Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task A_refused_token_still_signs_the_user_out()
    {
        var auth = new RecordingAuthentication();
        var api = new StubApi(auth, _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var page = CreatePage(api, auth);

        var result = await page.OnPostAsync();

        Assert.Single(api.Requests);
        Assert.True(auth.SignedOut);
        Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task An_unreachable_api_still_signs_the_user_out()
    {
        var auth = new RecordingAuthentication();
        var api = new StubApi(auth, _ => throw new HttpRequestException("connection refused"));
        var page = CreatePage(api, auth);

        var result = await page.OnPostAsync();

        Assert.True(auth.SignedOut);
        Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(result).PageName);
    }

    [Fact]
    public async Task A_server_error_from_the_api_still_signs_the_user_out()
    {
        var auth = new RecordingAuthentication();
        var api = new StubApi(auth, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var page = CreatePage(api, auth);

        await page.OnPostAsync();

        Assert.True(auth.SignedOut);
    }

    private static LogoutModel CreatePage(StubApi api, RecordingAuthentication auth)
    {
        var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(AuthConstants.AccessTokenClaim, "test-token")], "Cookies")),
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var handler = new AccessTokenHandler(accessor) { InnerHandler = api };
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

        return new LogoutModel(client, new SignInService(accessor), NullLogger<LogoutModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };
    }

    private sealed record Sent(HttpMethod Method, string Path, string? Authorization, bool SignedOutBefore);

    /// <summary>Stands in for the API and records each request, and whether the cookie was already gone then.</summary>
    private sealed class StubApi(RecordingAuthentication auth, Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Sent> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(new Sent(request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString(), auth.SignedOut));
            return Task.FromResult(respond(request));
        }
    }

    private sealed class RecordingAuthentication : IAuthenticationService
    {
        public bool SignedOut { get; private set; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }
}
