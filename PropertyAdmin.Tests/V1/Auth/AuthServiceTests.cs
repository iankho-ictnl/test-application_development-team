using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Auth;
using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Tests.V1.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_issues_an_access_token_with_roles_permissions_and_owner()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Reader", "ReadOwn", "Reports");
        var owner = new PropertyAdmin.Data.Owner { Name = "Owner" };
        host.Db.Owners.Add(owner);
        await host.Db.SaveChangesAsync();
        var user = await host.CreateUserAsync("reader", owner.Id, "Reader");
        var handler = new PropertyAdmin.Logic.V1.Auth.Login.LoginHandler(Service(host));

        var result = await handler.HandleAsync(new("reader", IdentityTestHost.Password), CancellationToken.None);

        Assert.Equal(LoginModel.Access(new AccessTokenModel("access-token", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc))), result.Login);
        var subject = host.Tokens.LastSubject!;
        Assert.Equal(user.Id, subject.UserId);
        Assert.Equal("reader", subject.UserName);
        Assert.Equal(owner.Id, subject.OwnerId);
        Assert.Equal(["Reader"], subject.Roles);
        Assert.Equal(["ReadOwn", "Reports"], subject.Permissions.Order());
    }

    [Fact]
    public async Task Login_fails_for_an_unknown_user_or_a_wrong_password_and_counts_the_failure()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("staff");
        var service = Service(host);

        Assert.Null(await service.LoginAsync("nobody", IdentityTestHost.Password, CancellationToken.None));
        Assert.Null(await service.LoginAsync("staff", "wrong-password", CancellationToken.None));
        Assert.Equal(1, await host.Users.GetAccessFailedCountAsync(user));
        Assert.Null(host.Tokens.LastSubject);
    }

    [Fact]
    public async Task Login_with_an_unknown_or_locked_out_user_name_still_runs_a_password_hash_check()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("locked");
        await host.Users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
        var hasher = new CountingPasswordHasher(host.Users.PasswordHasher);
        host.Users.PasswordHasher = hasher;
        var service = Service(host);

        Assert.Null(await service.LoginAsync("nobody", IdentityTestHost.Password, CancellationToken.None));
        Assert.Equal(1, hasher.Verifications);
        Assert.Null(await service.LoginAsync("locked", IdentityTestHost.Password, CancellationToken.None));
        Assert.Equal(2, hasher.Verifications);
        Assert.Equal(0, await host.Users.GetAccessFailedCountAsync(user));
        Assert.Null(host.Tokens.LastSubject);
    }

    [Fact]
    public async Task Login_with_two_factor_returns_a_challenge_that_a_recovery_code_completes()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("twofactor");
        await host.Users.SetTwoFactorEnabledAsync(user, true);
        var recoveryCode = (await host.Users.GenerateNewTwoFactorRecoveryCodesAsync(user, 1))!.Single();
        var service = Service(host);

        var challenge = (await service.LoginAsync("twofactor", IdentityTestHost.Password, CancellationToken.None))!;
        Assert.True(challenge.RequiresTwoFactor);
        Assert.Null(challenge.AccessToken);

        var handler = new PropertyAdmin.Logic.V1.Auth.LoginTwoFactor.LoginTwoFactorHandler(service);
        Assert.Null((await handler.HandleAsync(new("not-a-challenge", recoveryCode), CancellationToken.None)).Login);
        Assert.Null((await handler.HandleAsync(new(challenge.ChallengeToken!, "000000"), CancellationToken.None)).Login);
        var signedIn = (await handler.HandleAsync(new(challenge.ChallengeToken!, recoveryCode), CancellationToken.None)).Login!;
        Assert.False(signedIn.RequiresTwoFactor);
        Assert.Equal("access-token", signedIn.AccessToken);
    }

    [Fact]
    public async Task Two_factor_setup_creates_a_key_once_and_enable_refuses_a_wrong_code()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("setup");
        var service = Service(host);
        var get = new PropertyAdmin.Logic.V1.Auth.GetTwoFactor.GetTwoFactorHandler(service);

        var first = (await get.HandleAsync(new(user.Id), CancellationToken.None)).Setup!;
        var second = (await get.HandleAsync(new(user.Id), CancellationToken.None)).Setup!;
        Assert.Equal(first.SharedKey, second.SharedKey);
        Assert.StartsWith("otpauth://totp/PropertyAdmin:setup?secret=", first.AuthenticatorUri);
        Assert.False(first.Enabled);
        Assert.Null((await get.HandleAsync(new("unknown"), CancellationToken.None)).Setup);

        var enable = new PropertyAdmin.Logic.V1.Auth.EnableTwoFactor.EnableTwoFactorHandler(service);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => enable.HandleAsync(new(user.Id, "000 000"), CancellationToken.None));
        Assert.Equal("De code is ongeldig.", error.Message);
        Assert.Null((await enable.HandleAsync(new("unknown", "000000"), CancellationToken.None)).RecoveryCodes);
    }

    [Fact]
    public async Task Two_factor_setup_hides_the_key_and_uri_once_two_factor_is_on()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("enabled");
        await host.Users.ResetAuthenticatorKeyAsync(user);
        await host.Users.SetTwoFactorEnabledAsync(user, true);
        var get = new PropertyAdmin.Logic.V1.Auth.GetTwoFactor.GetTwoFactorHandler(Service(host));

        var setup = (await get.HandleAsync(new(user.Id), CancellationToken.None)).Setup!;

        Assert.Equal(new TwoFactorSetupModel(null, null, true), setup);
        Assert.Equal(new PropertyAdmin.Api.V1.Auth.GetTwoFactor.Response(null, null, true),
            PropertyAdmin.Api.V1.Auth.GetTwoFactor.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Auth.GetTwoFactor.Result(setup)));
    }

    [Fact]
    public async Task Disable_two_factor_with_the_password_turns_it_off_and_resets_the_key()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await TwoFactorUserAsync(host, "disable");
        var oldKey = await host.Users.GetAuthenticatorKeyAsync(user);
        var handler = new PropertyAdmin.Logic.V1.Auth.DisableTwoFactor.DisableTwoFactorHandler(Service(host));

        Assert.True((await handler.HandleAsync(new(user.Id, IdentityTestHost.Password, null), CancellationToken.None)).Found);
        Assert.False(await host.Users.GetTwoFactorEnabledAsync(user));
        Assert.NotEqual(oldKey, await host.Users.GetAuthenticatorKeyAsync(user));
        Assert.False((await handler.HandleAsync(new("unknown", IdentityTestHost.Password, null), CancellationToken.None)).Found);
    }

    [Fact]
    public async Task Disable_two_factor_with_an_authenticator_code_turns_it_off()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await TwoFactorUserAsync(host, "disablecode");
        var code = PropertyAdmin.Tests.Characterization.AuthTests.CreateAuthenticatorCode((await host.Users.GetAuthenticatorKeyAsync(user))!);

        Assert.True(await Service(host).DisableTwoFactorAsync(user.Id, null, code, CancellationToken.None));
        Assert.False(await host.Users.GetTwoFactorEnabledAsync(user));
    }

    [Fact]
    public async Task Disable_two_factor_refuses_no_credential_a_wrong_one_and_a_recovery_code_with_one_message()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await TwoFactorUserAsync(host, "disablewrong");
        var recoveryCode = (await host.Users.GenerateNewTwoFactorRecoveryCodesAsync(user, 1))!.Single();
        var service = Service(host);

        var none = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DisableTwoFactorAsync(user.Id, null, " ", CancellationToken.None));
        Assert.Equal(AuthService.ReauthenticationRequired, none.Message);
        Assert.Equal(0, await host.Users.GetAccessFailedCountAsync(user));

        foreach (var (password, code) in new (string?, string?)[] { ("wrong-password", null), (null, "000000"), (null, recoveryCode), (IdentityTestHost.Password, "000000") })
        {
            var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DisableTwoFactorAsync(user.Id, password, code, CancellationToken.None));
            Assert.Equal(AuthService.ReauthenticationFailed, error.Message);
        }

        Assert.Equal(4, await host.Users.GetAccessFailedCountAsync(user));
        Assert.True(await host.Users.GetTwoFactorEnabledAsync(user));
        Assert.Equal(1, await host.Users.CountRecoveryCodesAsync(user));
    }

    [Fact]
    public async Task Disable_two_factor_locks_out_at_the_fifth_failure_and_then_refuses_before_any_check()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await TwoFactorUserAsync(host, "disablelock");
        var service = Service(host);

        for (var attempt = 0; attempt < 5; attempt++)
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.DisableTwoFactorAsync(user.Id, "wrong-password", null, CancellationToken.None));

        Assert.True(await host.Users.IsLockedOutAsync(user));
        Assert.False(await service.DisableTwoFactorAsync(user.Id, IdentityTestHost.Password, null, CancellationToken.None));
        Assert.True(await host.Users.GetTwoFactorEnabledAsync(user));
    }

    [Fact]
    public async Task Disable_two_factor_while_it_is_off_changes_nothing()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("alreadyoff");
        var stamp = await host.Users.GetSecurityStampAsync(user);

        Assert.True(await Service(host).DisableTwoFactorAsync(user.Id, IdentityTestHost.Password, null, CancellationToken.None));

        Assert.Null(await host.Users.GetAuthenticatorKeyAsync(user));
        Assert.Equal(stamp, await host.Users.GetSecurityStampAsync(user));
    }

    [Fact]
    public void Disable_two_factor_mapping_copies_every_field() =>
        Assert.Equal(new PropertyAdmin.Logic.V1.Auth.DisableTwoFactor.Command("id", "secret", "123456"),
            PropertyAdmin.Api.V1.Auth.DisableTwoFactor.Mapping.ToCommand(new PropertyAdmin.Api.V1.Auth.DisableTwoFactor.Request("secret", "123456"), "id"));

    private static async Task<PropertyAdmin.Data.ApplicationUser> TwoFactorUserAsync(IdentityTestHost host, string userName)
    {
        var user = await host.CreateUserAsync(userName);
        await host.Users.ResetAuthenticatorKeyAsync(user);
        await host.Users.SetTwoFactorEnabledAsync(user, true);
        return user;
    }

    [Fact]
    public async Task Change_password_refuses_a_wrong_current_password_and_changes_it_otherwise()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("password");
        var handler = new PropertyAdmin.Logic.V1.Auth.ChangePassword.ChangePasswordHandler(Service(host));

        await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(new(user.Id, "wrong-password", "Another-Password-456!"), CancellationToken.None));
        Assert.True((await handler.HandleAsync(new(user.Id, IdentityTestHost.Password, "Another-Password-456!"), CancellationToken.None)).Found);
        Assert.True(await host.Users.CheckPasswordAsync(user, "Another-Password-456!"));
        Assert.False((await handler.HandleAsync(new("unknown", "a", "b"), CancellationToken.None)).Found);
    }

    [Fact]
    public async Task Logout_renews_the_security_stamp_and_reports_an_unknown_user()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("logout");
        var stamp = await host.Users.GetSecurityStampAsync(user);
        var handler = new PropertyAdmin.Logic.V1.Auth.Logout.LogoutHandler(Service(host));

        Assert.True((await handler.HandleAsync(PropertyAdmin.Api.V1.Auth.Logout.Mapping.ToCommand(user.Id), CancellationToken.None)).Found);
        Assert.NotEqual(stamp, await host.Users.GetSecurityStampAsync(user));
        Assert.False((await handler.HandleAsync(new("unknown"), CancellationToken.None)).Found);
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("cancel");
        var service = Service(host);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PropertyAdmin.Logic.V1.Auth.Login.LoginHandler(service)
            .HandleAsync(new("cancel", IdentityTestHost.Password), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ChangePasswordAsync(user.Id, IdentityTestHost.Password, "Another-Password-456!", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DisableTwoFactorAsync(user.Id, "wrong-password", null, cancellation.Token));
        var stamp = await host.Users.GetSecurityStampAsync(user);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LogoutAsync(user.Id, cancellation.Token));
        Assert.Equal(stamp, await host.Users.GetSecurityStampAsync(user));
        Assert.Equal(0, await host.Users.GetAccessFailedCountAsync(user));
        Assert.Null(host.Tokens.LastSubject);
        Assert.True(await host.Users.CheckPasswordAsync(user, IdentityTestHost.Password));
    }

    [Fact]
    public void Login_mapping_copies_every_field()
    {
        var expires = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var response = PropertyAdmin.Api.V1.Auth.Login.Mapping.ToResponse(
            new PropertyAdmin.Logic.V1.Auth.Login.Result(new LoginModel(false, null, "token", expires)));

        Assert.Equal(new PropertyAdmin.Api.V1.Auth.Login.Response(false, null, "token", expires), response);
        Assert.Equal(new PropertyAdmin.Logic.V1.Auth.Login.Command("user", "secret"),
            PropertyAdmin.Api.V1.Auth.Login.Mapping.ToCommand(new PropertyAdmin.Api.V1.Auth.Login.Request("user", "secret")));
    }

    private static AuthService Service(IdentityTestHost host) => new(host.Users, host.Roles, host.Tokens);

    private sealed class CountingPasswordHasher(Microsoft.AspNetCore.Identity.IPasswordHasher<PropertyAdmin.Data.ApplicationUser> inner)
        : Microsoft.AspNetCore.Identity.IPasswordHasher<PropertyAdmin.Data.ApplicationUser>
    {
        public int Verifications { get; private set; }

        public string HashPassword(PropertyAdmin.Data.ApplicationUser user, string password) => inner.HashPassword(user, password);

        public Microsoft.AspNetCore.Identity.PasswordVerificationResult VerifyHashedPassword(PropertyAdmin.Data.ApplicationUser user, string hashedPassword, string providedPassword)
        {
            Verifications++;
            return inner.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
