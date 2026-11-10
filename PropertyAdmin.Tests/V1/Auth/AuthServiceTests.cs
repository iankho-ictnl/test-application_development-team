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
    public async Task Disable_two_factor_turns_it_off_and_resets_the_key()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var user = await host.CreateUserAsync("disable");
        await host.Users.SetTwoFactorEnabledAsync(user, true);
        await host.Users.ResetAuthenticatorKeyAsync(user);
        var oldKey = await host.Users.GetAuthenticatorKeyAsync(user);
        var handler = new PropertyAdmin.Logic.V1.Auth.DisableTwoFactor.DisableTwoFactorHandler(Service(host));

        Assert.True((await handler.HandleAsync(new(user.Id), CancellationToken.None)).Found);
        Assert.False(await host.Users.GetTwoFactorEnabledAsync(user));
        Assert.NotEqual(oldKey, await host.Users.GetAuthenticatorKeyAsync(user));
        Assert.False((await handler.HandleAsync(new("unknown"), CancellationToken.None)).Found);
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
}
