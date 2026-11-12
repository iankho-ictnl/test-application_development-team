using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>T-042: current behaviour of api/auth, api/users and api/roles (moved by T-036).</summary>
[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory api)
{
    [Fact]
    public async Task Login_success_returns_access_token_shape()
    {
        var json = await Expect(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = ApiFactory.AdminUserName, password = ApiFactory.Password }), HttpStatusCode.OK);
        HasShape(json, "requiresTwoFactor", "challengeToken", "accessToken", "expiresUtc");
        Assert.False(json.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Wrong_password_and_unknown_user_are_401()
    {
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = ApiFactory.AdminUserName, password = "wrong-password" }), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName = "nobody", password = "x" }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_without_fields_is_validation_problem() =>
        await ExpectValidationProblem(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { }));

    [Fact]
    public async Task Invalid_challenge_token_is_401() =>
        await ExpectStatus(await api.Anonymous().PostAsJsonAsync("/api/auth/login-2fa", new { challengeToken = "not-a-token", code = "123456" }), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Invalid_token_is_401() =>
        await ExpectStatus(await api.WithToken("not-a-token").GetAsync("/api/auth/2fa"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Two_factor_setup_enable_with_wrong_code_and_disable()
    {
        var userName = api.Unique("tweefactor");
        await api.CreateUserAsync(userName, "Staff");
        var client = api.WithToken(await api.LoginAsync(userName, ApiFactory.Password));

        var setup = await Expect(await client.GetAsync("/api/auth/2fa"), HttpStatusCode.OK);
        HasShape(setup, "sharedKey", "authenticatorUri", "enabled");
        Assert.False(setup.GetProperty("enabled").GetBoolean());
        Assert.StartsWith("otpauth://totp/PropertyAdmin:", setup.GetProperty("authenticatorUri").GetString());

        // Current behaviour: the first set-up creates the authenticator key, which changes the
        // security stamp, so the token used for it is revoked (401); the user signs in again.
        await ExpectStatus(await client.GetAsync("/api/auth/2fa"), HttpStatusCode.Unauthorized);
        client = api.WithToken(await api.LoginAsync(userName, ApiFactory.Password));
        Assert.Equal(setup.GetProperty("sharedKey").GetString(), (await Expect(await client.GetAsync("/api/auth/2fa"), HttpStatusCode.OK)).GetProperty("sharedKey").GetString());

        await ExpectProblem(await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = "000000" }), HttpStatusCode.BadRequest, DomainTitle);
        // T-044 S-19 (D-036): disable needs the current password or an authenticator code.
        await ExpectStatus(await client.PostAsJsonAsync("/api/auth/2fa/disable", new { password = ApiFactory.Password }), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Enable_two_factor_and_complete_challenge_return_current_response_shapes()
    {
        var userName = api.Unique("tweefactor");
        await api.CreateUserAsync(userName, "Staff");
        var client = api.WithToken(await api.LoginAsync(userName, ApiFactory.Password));

        var setup = await Expect(await client.GetAsync("/api/auth/2fa"), HttpStatusCode.OK);
        HasShape(setup, "sharedKey", "authenticatorUri", "enabled");
        client = api.WithToken(await api.LoginAsync(userName, ApiFactory.Password));

        var code = CreateAuthenticatorCode(setup.GetProperty("sharedKey").GetString()!);
        var enabled = await Expect(await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code }), HttpStatusCode.OK);
        HasShape(enabled, "recoveryCodes");
        Assert.Equal(10, enabled.GetProperty("recoveryCodes").GetArrayLength());

        var challenge = await Expect(await api.Anonymous().PostAsJsonAsync("/api/auth/login", new { userName, password = ApiFactory.Password }), HttpStatusCode.OK);
        HasShape(challenge, "requiresTwoFactor", "challengeToken", "accessToken", "expiresUtc");
        Assert.True(challenge.GetProperty("requiresTwoFactor").GetBoolean());
        var challengeToken = challenge.GetProperty("challengeToken").GetString();

        code = CreateAuthenticatorCode(setup.GetProperty("sharedKey").GetString()!);
        var signedIn = await Expect(await api.Anonymous().PostAsJsonAsync("/api/auth/login-2fa", new { challengeToken, code }), HttpStatusCode.OK);
        HasShape(signedIn, "requiresTwoFactor", "challengeToken", "accessToken", "expiresUtc");
        Assert.False(signedIn.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(string.IsNullOrEmpty(signedIn.GetProperty("accessToken").GetString()));
    }

    internal static string CreateAuthenticatorCode(string sharedKey)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in sharedKey.Replace(" ", "", StringComparison.Ordinal))
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                key.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        using var hmac = new HMACSHA1(key.ToArray());
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & int.MaxValue;
        return (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task Change_password_wrong_current_is_400_then_success_is_204()
    {
        var userName = api.Unique("wachtwoord");
        await api.CreateUserAsync(userName, "Viewer");
        var client = api.WithToken(await api.LoginAsync(userName, ApiFactory.Password));

        await ExpectProblem(await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = "wrong-password", newPassword = "Another-Password-456!" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await client.PostAsJsonAsync("/api/auth/password", new { currentPassword = ApiFactory.Password, newPassword = "Another-Password-456!" }), HttpStatusCode.NoContent);
        await api.LoginAsync(userName, "Another-Password-456!");
    }
}

[Collection(ApiCollection.Name)]
public class UsersTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_is_401_and_without_permission_is_403()
    {
        await ExpectStatus(await api.Anonymous().GetAsync("/api/users"), HttpStatusCode.Unauthorized);
        await ExpectStatus(await (await api.As("Manager")).GetAsync("/api/users"), HttpStatusCode.Forbidden);
        await ExpectStatus(await (await api.As("Manager")).GetAsync("/api/roles"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_and_get_return_user_shape()
    {
        var admin = await api.As("Admin");
        var list = await Expect(await admin.GetAsync("/api/users"), HttpStatusCode.OK);
        var me = list.EnumerateArray().Single(u => u.GetProperty("userName").GetString() == ApiFactory.AdminUserName);
        HasShape(me, UserShape);
        Assert.Equal(["Admin"], me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        HasShape(await Expect(await admin.GetAsync($"/api/users/{me.GetProperty("id").GetString()}"), HttpStatusCode.OK), UserShape);
        await ExpectStatus(await admin.GetAsync("/api/users/unknown-id"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_update_reset_sign_out_and_delete()
    {
        var admin = await api.As("Admin");
        var userName = api.Unique("nieuw");
        var response = await admin.PostAsJsonAsync("/api/users", new { userName, email = $"{userName}@example.test", password = ApiFactory.Password, roles = new[] { "Staff" } });
        var created = await Expect(response, HttpStatusCode.Created);
        HasShape(created, UserShape);
        var id = created.GetProperty("id").GetString();

        var updated = await Expect(await admin.PutAsJsonAsync($"/api/users/{id}", new { email = $"{userName}@example.test", roles = new[] { "Viewer" }, lockedOut = true }), HttpStatusCode.OK);
        Assert.True(updated.GetProperty("lockedOut").GetBoolean());
        Assert.Equal(["Viewer"], updated.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));

        await ExpectStatus(await admin.PostAsync($"/api/users/{id}/reset-2fa", null), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.PostAsync($"/api/users/{id}/sign-out", null), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/users/{id}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/users/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_creates_are_400()
    {
        var admin = await api.As("Admin");
        await ExpectProblem(await admin.PostAsJsonAsync("/api/users", new { userName = api.Unique("u"), email = "u@example.test", password = ApiFactory.Password, roles = new[] { "NoSuchRole" } }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/users", new { userName = api.Unique("u"), email = "u2@example.test", password = "short", roles = new[] { "Staff" } }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/users", new { userName = api.Unique("u"), email = "not-an-email", password = ApiFactory.Password, roles = new[] { "Staff" } }));
    }

    [Fact]
    public async Task Unknown_user_actions_are_404()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await admin.PutAsJsonAsync("/api/users/unknown-id", new { email = "x@example.test", roles = Array.Empty<string>(), lockedOut = false }), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.PostAsync("/api/users/unknown-id/reset-2fa", null), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.PostAsync("/api/users/unknown-id/sign-out", null), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Last_user_admin_cannot_lose_the_permission()
    {
        var admin = await api.As("Admin");
        var list = await Expect(await admin.GetAsync("/api/users"), HttpStatusCode.OK);
        var adminIds = list.EnumerateArray().Where(u => u.GetProperty("roles").EnumerateArray().Any(r => r.GetString() == "Admin") && !u.GetProperty("lockedOut").GetBoolean()).ToList();
        Assert.Single(adminIds);
        var id = adminIds[0].GetProperty("id").GetString();
        await ExpectProblem(await admin.PutAsJsonAsync($"/api/users/{id}", new { email = "admin.test@example.test", roles = new[] { "Viewer" }, lockedOut = false }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.DeleteAsync($"/api/users/{id}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Roles_list_create_update_delete()
    {
        var admin = await api.As("Admin");
        var roles = await Expect(await admin.GetAsync("/api/roles"), HttpStatusCode.OK);
        var adminRole = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Admin");
        HasShape(adminRole, "name", "permissions", "userCount");

        var name = api.Unique("Rol");
        await ExpectStatus(await admin.PostAsJsonAsync("/api/roles", new { name, permissions = new[] { "Reports" } }), HttpStatusCode.NoContent);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/roles", new { name, permissions = new[] { "Reports" } }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/roles", new { name = api.Unique("Rol"), permissions = new[] { "Unknown" } }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await admin.PutAsJsonAsync($"/api/roles/{name}", new { name, permissions = new[] { "Reports", "ReadAll" } }), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/roles/NoSuchRole", new { name = "X", permissions = Array.Empty<string>() }), HttpStatusCode.NotFound);
        await ExpectProblem(await admin.DeleteAsync("/api/roles/Admin"), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await admin.DeleteAsync($"/api/roles/{name}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/roles/{name}"), HttpStatusCode.NotFound);
    }
}
