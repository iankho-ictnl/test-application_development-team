extern alias Frontend;

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Pages;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-001, security finding S-19 (UI), D-036: turning off two-factor sign-in on the profile page
/// needs the current password or an authenticator code.
/// </summary>
public sealed class ProfileTwoFactorDisableTests
{
    [Fact]
    public async Task Without_password_or_code_the_page_shows_a_field_error_and_does_not_call_the_api()
    {
        var api = new StubApi();
        var (page, auth) = CreatePage(api);

        var result = await page.OnPostDisableAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Contains(page.ModelState[nameof(ProfileModel.DisablePassword)]!.Errors,
            e => e.ErrorMessage == "Enter your current password or a code from your authenticator app.");
        Assert.DoesNotContain(api.Requests, r => r.Path == "/api/auth/2fa/disable");
        Assert.False(auth.SignedOut);
    }

    [Fact]
    public async Task With_the_password_the_api_gets_it_and_the_user_is_signed_out()
    {
        var api = new StubApi();
        var (page, auth) = CreatePage(api);
        page.DisablePassword = "Current-Pa55";

        var result = await page.OnPostDisableAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Account/Login", redirect.PageName);
        Assert.True(auth.SignedOut);
        var body = Assert.Single(api.Requests, r => r.Path == "/api/auth/2fa/disable").Body;
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Current-Pa55", json.RootElement.GetProperty("password").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("code").ValueKind);
    }

    [Fact]
    public async Task With_only_a_code_the_api_gets_the_trimmed_code_and_no_password()
    {
        var api = new StubApi();
        var (page, _) = CreatePage(api);
        page.DisableCode = " 123456 ";

        await page.OnPostDisableAsync();

        var body = Assert.Single(api.Requests, r => r.Path == "/api/auth/2fa/disable").Body;
        using var json = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("password").ValueKind);
        Assert.Equal("123456", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_wrong_password_or_code_from_the_api_shows_an_error_on_the_page_and_keeps_the_user_signed_in()
    {
        var api = new StubApi { DisableStatus = HttpStatusCode.BadRequest };
        var (page, auth) = CreatePage(api);
        page.DisableCode = "000000";

        var result = await page.OnPostDisableAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(page.ModelState[nameof(ProfileModel.DisablePassword)]!.Errors,
            e => e.ErrorMessage == "The password or code is incorrect.");
        Assert.False(auth.SignedOut);
    }

    private static (ProfileModel Page, RecordingAuthentication Auth) CreatePage(StubApi api)
    {
        var auth = new RecordingAuthentication();
        var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("http://api.test/") });

        var page = new ProfileModel(client, new SignInService(accessor), new EchoLocalizer())
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(httpContext, new NoTempDataProvider()),
        };
        return (page, auth);
    }

    /// <summary>Stands in for the API: two-factor is on (no key), disable answers with <see cref="DisableStatus"/>.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        public HttpStatusCode DisableStatus { get; init; } = HttpStatusCode.NoContent;
        public List<(string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add((path, body));
            return path switch
            {
                "/api/auth/2fa" => Json(HttpStatusCode.OK, """{"sharedKey":null,"authenticatorUri":null,"enabled":true}"""),
                "/api/auth/2fa/disable" when DisableStatus == HttpStatusCode.BadRequest =>
                    Json(DisableStatus, """{"title":"One or more validation errors occurred.","status":400}"""),
                "/api/auth/2fa/disable" => new HttpResponseMessage(DisableStatus),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class RecordingAuthentication : IAuthenticationService
    {
        public bool SignedOut { get; private set; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, System.Security.Claims.ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>Returns the resource key itself, so the tests check which text the page chose.</summary>
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
