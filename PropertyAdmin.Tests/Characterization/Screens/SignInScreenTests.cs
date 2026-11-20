using System.Net;
using System.Net.Http.Json;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>TC-10: sign-in, sign-out and the anonymous redirect of the admin frontend (US-1), with a real sign-in against the API.</summary>
[Collection(FrontendCollection.Name)]
[Trait("Requirement", "REQ-001")]
[Trait("TestCase", "TC-10")]
public sealed class SignInScreenTests(FrontendFactory web)
{
    public static TheoryData<string> ProtectedPages => new(
        "/", "/Profile", "/Owners", "/Owners/Details/1", "/Owners/Edit", "/Assets", "/Assets/Details/1", "/Assets/Edit",
        "/Expenses", "/Expenses/Edit", "/Activities", "/Activities/Edit", "/VatRates", "/Invoices", "/Invoices/Details/1",
        "/Invoices/Create", "/Reports", "/Admin/Users", "/Admin/Users/Edit", "/Admin/Roles");

    [Theory]
    [MemberData(nameof(ProtectedPages))]
    public async Task Anonymous_is_redirected_to_sign_in(string url) => await web.NewBrowser().ExpectSignInRedirect(url);

    [Fact]
    public async Task Sign_in_page_is_anonymous_and_has_the_sign_in_form()
    {
        var page = await web.NewBrowser().Page("/Account/Login");
        Assert.Contains("name=\"UserName\"", page);
        Assert.Contains("name=\"Password\"", page);
    }

    [Fact]
    public async Task Sign_in_returns_to_the_requested_page_and_sign_out_ends_the_session()
    {
        var browser = web.NewBrowser();
        var response = await browser.Submit("/Account/Login?ReturnUrl=%2FOwners", new() { ["UserName"] = ApiFactory.AdminUserName, ["Password"] = ApiFactory.Password });
        Browser.ExpectRedirect(response, "/Owners");
        await browser.Page("/Owners");

        Browser.ExpectRedirect(await browser.Submit("/Owners", new(), "/Account/Logout"), "/Account/Login");
        await browser.ExpectSignInRedirect("/Owners");
    }

    [Fact]
    public async Task Wrong_password_stays_on_the_sign_in_page_without_a_session()
    {
        var userName = web.Api.Unique("screen.wrong");
        await web.Api.CreateUserAsync(userName, "Viewer");
        var browser = web.NewBrowser();

        var response = await browser.Submit("/Account/Login", new() { ["UserName"] = userName, ["Password"] = "Not-the-password-1!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        await browser.ExpectSignInRedirect("/Owners");
    }

    [Fact]
    [Trait("Requirement", "REQ-019")]
    public async Task Eleventh_sign_in_attempt_from_one_client_ip_in_a_minute_is_refused_with_429()
    {
        // D-044 (S-17): 10 attempts per minute per client IP; the eleventh gets the localized 429 page.
        var browser = web.NewBrowser(System.Net.IPAddress.Parse("10.99.0.1"));
        var form = new Dictionary<string, string> { ["UserName"] = "screen.ratelimit", ["Password"] = "Not-the-password-1!" };
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await browser.Submit("/Account/Login", form)).StatusCode);
        }

        var refused = await browser.Submit("/Account/Login", form);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        // Another client IP is not affected.
        Assert.Equal(HttpStatusCode.OK, (await web.NewBrowser().Submit("/Account/Login", form)).StatusCode);
    }

    [Fact]
    public async Task Sign_in_ignores_a_return_url_to_another_site()
    {
        var response = await web.NewBrowser().Submit("/Account/Login?ReturnUrl=https%3A%2F%2Fevil.example%2F", new() { ["UserName"] = ApiFactory.AdminUserName, ["Password"] = ApiFactory.Password });
        var location = Browser.ExpectRedirect(response, "/");
        Assert.Equal("localhost", location.Host);
    }

    [Fact]
    [Trait("Requirement", "REQ-013")]
    public async Task Two_factor_user_signs_in_with_the_code_page()
    {
        var userName = web.Api.Unique("screen.2fa");
        await web.Api.CreateUserAsync(userName, "Staff");
        var api = web.Api.WithToken(await web.Api.LoginAsync(userName, ApiFactory.Password));
        var sharedKey = (await Http.Expect(await api.GetAsync("/api/auth/2fa"), HttpStatusCode.OK)).GetProperty("sharedKey").GetString()!;
        // The first set-up changes the security stamp (see AuthUsersTests), so sign in to the API again.
        api = web.Api.WithToken(await web.Api.LoginAsync(userName, ApiFactory.Password));
        await Http.Expect(await api.PostAsJsonAsync("/api/auth/2fa/enable", new { code = AuthTests.CreateAuthenticatorCode(sharedKey) }), HttpStatusCode.OK);
        var browser = web.NewBrowser();

        var response = await browser.Submit("/Account/Login", new() { ["UserName"] = userName, ["Password"] = ApiFactory.Password });
        Browser.ExpectRedirect(response, "/Account/LoginTwoFactor");
        response = await browser.Submit("/Account/LoginTwoFactor", new() { ["Code"] = AuthTests.CreateAuthenticatorCode(sharedKey) });

        Browser.ExpectRedirect(response, "/");
        await browser.Page("/Owners");
    }

    [Fact]
    public async Task Wrong_two_factor_code_returns_to_sign_in_without_a_session()
    {
        var userName = web.Api.Unique("screen.2fa");
        await web.Api.CreateUserAsync(userName, "Staff");
        var api = web.Api.WithToken(await web.Api.LoginAsync(userName, ApiFactory.Password));
        var sharedKey = (await Http.Expect(await api.GetAsync("/api/auth/2fa"), HttpStatusCode.OK)).GetProperty("sharedKey").GetString()!;
        api = web.Api.WithToken(await web.Api.LoginAsync(userName, ApiFactory.Password));
        await Http.Expect(await api.PostAsJsonAsync("/api/auth/2fa/enable", new { code = AuthTests.CreateAuthenticatorCode(sharedKey) }), HttpStatusCode.OK);
        var browser = web.NewBrowser();
        Browser.ExpectRedirect(await browser.Submit("/Account/Login", new() { ["UserName"] = userName, ["Password"] = ApiFactory.Password }), "/Account/LoginTwoFactor");

        var response = await browser.Submit("/Account/LoginTwoFactor", new() { ["Code"] = "000000" });

        Browser.ExpectRedirect(response, "/Account/Login");
        await browser.ExpectSignInRedirect("/Owners");
    }

    [Fact]
    public async Task User_signed_out_everywhere_by_an_admin_is_sent_to_sign_in_at_the_next_page()
    {
        var userName = web.Api.Unique("screen.revoked");
        await web.Api.CreateUserAsync(userName, "Viewer");
        var browser = await web.SignIn(userName);
        await browser.Page("/Owners");
        var admin = await web.Api.As("Admin");
        var users = await Http.Expect(await admin.GetAsync("/api/users"), HttpStatusCode.OK);
        var id = users.EnumerateArray().Single(u => u.GetProperty("userName").GetString() == userName).GetProperty("id").GetString();
        await Http.ExpectStatus(await admin.PostAsync($"/api/users/{id}/sign-out", null), HttpStatusCode.NoContent);

        Browser.ExpectRedirect(await browser.GetAsync("/Owners"), "/Account/Login");
        await browser.ExpectSignInRedirect("/Owners");
    }

    [Fact]
    [Trait("Requirement", "REQ-013")]
    public async Task Profile_shows_the_two_factor_set_up_and_its_first_visit_ends_the_api_session()
    {
        var userName = web.Api.Unique("screen.profile");
        await web.Api.CreateUserAsync(userName, "Viewer");
        var browser = await web.SignIn(userName);

        var page = await browser.Page("/Profile");
        Assert.Contains("data:image/png;base64,", page);

        // Current behaviour of BUG-13 (testing.md), not the expected behaviour: the first set-up
        // creates the authenticator key and changes the security stamp, so the next API call is
        // 401 and the frontend signs the user out. When BUG-13 is fixed, this line changes to
        // await browser.Page("/Owners").
        Browser.ExpectRedirect(await browser.GetAsync("/Owners"), "/Account/Login");
        browser = await web.SignIn(userName);
        await browser.Page("/Profile");
        await browser.Page("/Owners");
    }
}
