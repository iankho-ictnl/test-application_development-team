using System.Net;
using System.Net.Http.Json;
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
    public async Task Auth_v1_refuses_bad_credentials_and_anonymous_own_account_calls()
    {
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login", new { userName = ApiFactory.AdminUserName, password = "wrong-password" }), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/login-2fa", new { challengeToken = "not-a-token", code = "123456" }), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/auth/2fa"), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/v1/auth/password", new { currentPassword = "a", newPassword = "b" }), HttpStatusCode.Unauthorized);
    }
}
