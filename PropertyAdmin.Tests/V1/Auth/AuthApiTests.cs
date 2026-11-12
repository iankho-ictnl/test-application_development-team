using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PropertyAdmin.Data;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Auth;

[Collection(ApiCollection.Name)]
public sealed class AuthApiTests(ApiFactory api)
{
    [Fact]
    public async Task Login_v1_returns_a_token_that_the_own_account_endpoints_accept_until_the_stamp_changes()
    {
        var userName = api.Unique("authv1");
        await api.CreateUserAsync(userName, "Viewer");

        var login = await Expect(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { userName, password = ApiFactory.Password }), HttpStatusCode.OK);
        Assert.False(login.GetProperty("requiresTwoFactor").GetBoolean());
        var client = api.WithToken(login.GetProperty("accessToken").GetString()!);

        var setup = await Expect(await client.GetAsync("/api/v1/auth/2fa"), HttpStatusCode.OK);
        Assert.False(setup.GetProperty("enabled").GetBoolean());
        // The first set-up creates the authenticator key, which renews the security stamp (as on /api/auth).
        await ExpectStatus(await client.PostAsync("/api/v1/auth/2fa/disable", null), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Two_factor_setup_v1_returns_no_key_once_two_factor_is_on()
    {
        var client = await SignInWithTwoFactorAsync(api.Unique("twofactoron"));

        var setup = await Expect(await client.GetAsync("/api/v1/auth/2fa"), HttpStatusCode.OK);

        Assert.True(setup.GetProperty("enabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, setup.GetProperty("sharedKey").ValueKind);
        Assert.Equal(JsonValueKind.Null, setup.GetProperty("authenticatorUri").ValueKind);
    }

    [Fact]
    public async Task Disable_two_factor_needs_the_password_or_a_code_and_gives_one_400_for_both_wrong()
    {
        var client = await SignInWithTwoFactorAsync(api.Unique("disablev1"));

        // No JSON body at all (the profile page until T-046) is refused before the handler.
        await ExpectStatus(await client.PostAsync("/api/v1/auth/2fa/disable", null), HttpStatusCode.UnsupportedMediaType);
        await ExpectProblem(await client.PostAsJsonAsync("/api/v1/auth/2fa/disable", new { }), HttpStatusCode.BadRequest, DomainTitle);
        var wrongPassword = await ExpectProblem(await client.PostAsJsonAsync("/api/v1/auth/2fa/disable", new { password = "wrong-password" }), HttpStatusCode.BadRequest, DomainTitle);
        var wrongCode = await ExpectProblem(await client.PostAsJsonAsync("/api/auth/2fa/disable", new { code = "000000" }), HttpStatusCode.BadRequest, DomainTitle);
        Assert.Equal(wrongPassword.GetProperty("detail").GetString(), wrongCode.GetProperty("detail").GetString());

        await ExpectStatus(await client.PostAsJsonAsync("/api/v1/auth/2fa/disable", new { password = ApiFactory.Password }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Too_long_credentials_are_refused_with_a_validation_400_on_both_routes()
    {
        var userName101 = new string('u', 101);
        var password129 = new string('p', 129);
        var code65 = new string('1', 65);
        var token4097 = new string('t', 4097);
        var anonymous = api.Anonymous();
        var signedIn = await api.As("Viewer");
        var admin = await api.As("Admin");

        foreach (var route in new[] { "/api/v1/auth", "/api/auth" })
        {
            await ExpectValidationProblem(await anonymous.PostAsJsonAsync($"{route}/login", new { userName = userName101, password = ApiFactory.Password }));
            await ExpectValidationProblem(await anonymous.PostAsJsonAsync($"{route}/login", new { userName = ApiFactory.AdminUserName, password = password129 }));
            await ExpectValidationProblem(await anonymous.PostAsJsonAsync($"{route}/login-2fa", new { challengeToken = token4097, code = "123456" }));
            await ExpectValidationProblem(await anonymous.PostAsJsonAsync($"{route}/login-2fa", new { challengeToken = "challenge", code = code65 }));
            await ExpectValidationProblem(await signedIn.PostAsJsonAsync($"{route}/2fa/enable", new { code = code65 }));
            await ExpectValidationProblem(await signedIn.PostAsJsonAsync($"{route}/2fa/disable", new { password = password129 }));
            await ExpectValidationProblem(await signedIn.PostAsJsonAsync($"{route}/2fa/disable", new { code = code65 }));
            await ExpectValidationProblem(await signedIn.PostAsJsonAsync($"{route}/password", new { currentPassword = password129, newPassword = "Another-Password-456!" }));
            await ExpectValidationProblem(await signedIn.PostAsJsonAsync($"{route}/password", new { currentPassword = ApiFactory.Password, newPassword = password129 }));
        }
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/v1/users",
            new { userName = api.Unique("longpw"), email = "longpw@example.test", password = password129, ownerId = (int?)null, roles = new[] { "Viewer" } }));
    }

    [Theory]
    [InlineData("/api/v1/auth")]
    [InlineData("/api/auth")]
    public async Task Logout_refuses_every_token_of_the_user_afterwards(string route)
    {
        var userName = api.Unique("logout");
        await api.CreateUserAsync(userName, "Viewer");
        var first = await SignInAsync(userName);
        var second = await SignInAsync(userName);
        await ExpectStatus(await second.GetAsync("/api/v1/owners"), HttpStatusCode.OK);

        await ExpectStatus(await first.PostAsync($"{route}/logout", null), HttpStatusCode.NoContent);

        foreach (var client in new[] { first, second })
        {
            await ExpectStatus(await client.GetAsync("/api/v1/auth/2fa"), HttpStatusCode.Unauthorized);
            await ExpectStatus(await client.GetAsync("/api/v1/owners"), HttpStatusCode.Unauthorized);
            await ExpectStatus(await client.PostAsync($"{route}/logout", null), HttpStatusCode.Unauthorized);
        }
        await ExpectStatus(await api.Anonymous().PostAsync($"{route}/logout", null), HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> SignInAsync(string userName)
    {
        var login = await Expect(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { userName, password = ApiFactory.Password }), HttpStatusCode.OK);
        return api.WithToken(login.GetProperty("accessToken").GetString()!);
    }

    /// <summary>A user with two-factor sign-in on, signed in through login and login-2fa with a recovery code.</summary>
    private async Task<HttpClient> SignInWithTwoFactorAsync(string userName)
    {
        await api.CreateUserAsync(userName, "Viewer");
        string recoveryCode;
        using (var scope = api.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByNameAsync(userName))!;
            await users.ResetAuthenticatorKeyAsync(user);
            await users.SetTwoFactorEnabledAsync(user, true);
            recoveryCode = (await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 1))!.Single();
        }
        var challenge = await Expect(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { userName, password = ApiFactory.Password }), HttpStatusCode.OK);
        var login = await Expect(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login-2fa",
            new { challengeToken = challenge.GetProperty("challengeToken").GetString(), code = recoveryCode }), HttpStatusCode.OK);
        return api.WithToken(login.GetProperty("accessToken").GetString()!);
    }

    [Fact]
    public async Task Auth_v1_refuses_bad_credentials_and_anonymous_own_account_calls()
    {
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { userName = ApiFactory.AdminUserName, password = "wrong-password" }), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login-2fa", new { challengeToken = "not-a-token", code = "123456" }), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/auth/2fa"), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "a", newPassword = "b" }), HttpStatusCode.Unauthorized);
    }
}
