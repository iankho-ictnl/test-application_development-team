using PropertyAdmin.Contracts;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Users;
using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Tests.V1.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Create_then_get_by_id_and_get_all_return_the_user_with_sorted_roles()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Staff");
        await host.CreateRoleAsync("Admin", Permissions.Users);
        var service = Service(host);

        var created = (await new PropertyAdmin.Logic.V1.Users.Create.CreateHandler(service)
            .HandleAsync(new(new UserCreateModel("new", "new@example.test", IdentityTestHost.Password, null, ["Staff", "Admin", "Staff"])), CancellationToken.None)).User;

        Assert.Equal(["Admin", "Staff"], created.Roles);
        Assert.False(created.LockedOut);
        var found = (await new PropertyAdmin.Logic.V1.Users.GetById.GetByIdHandler(service).HandleAsync(new(created.Id), CancellationToken.None)).User!;
        Assert.Equal(created with { Roles = found.Roles }, found);
        Assert.Equal(created.Roles, found.Roles);
        var listed = Assert.Single((await new PropertyAdmin.Logic.V1.Users.GetAll.GetAllHandler(service).HandleAsync(new(), CancellationToken.None)).Users);
        Assert.Equal(created with { Roles = listed.Roles }, listed);
        Assert.Null((await new PropertyAdmin.Logic.V1.Users.GetById.GetByIdHandler(service).HandleAsync(new("unknown"), CancellationToken.None)).User);
    }

    [Fact]
    public async Task Create_refuses_an_unknown_role_an_unknown_owner_and_a_weak_password()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Staff");
        var service = Service(host);

        var role = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new("a", "a@example.test", IdentityTestHost.Password, null, ["NoSuchRole"]), CancellationToken.None));
        Assert.Equal("Rol 'NoSuchRole' bestaat niet.", role.Message);
        var owner = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new("b", "b@example.test", IdentityTestHost.Password, 999, ["Staff"]), CancellationToken.None));
        Assert.Equal("Eigenaar bestaat niet.", owner.Message);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new("c", "c@example.test", "short", null, ["Staff"]), CancellationToken.None));
        Assert.Empty(await service.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Update_changes_email_roles_and_lockout_and_renews_the_security_stamp()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        await host.CreateRoleAsync("Staff");
        await host.CreateRoleAsync("Viewer");
        await host.CreateUserAsync("admin", null, "Admin");
        var user = await host.CreateUserAsync("staff", null, "Staff");
        var stamp = user.SecurityStamp;
        var handler = new PropertyAdmin.Logic.V1.Users.Update.UpdateHandler(Service(host));

        var updated = (await handler.HandleAsync(new(user.Id, new UserUpdateModel("changed@example.test", null, ["Viewer"], true)), CancellationToken.None)).User!;

        Assert.Equal("changed@example.test", updated.Email);
        Assert.Equal(["Viewer"], updated.Roles);
        Assert.True(updated.LockedOut);
        Assert.NotEqual(stamp, (await host.Users.FindByIdAsync(user.Id))!.SecurityStamp);
        Assert.Null((await handler.HandleAsync(new("unknown", new UserUpdateModel("x@example.test", null, [], false)), CancellationToken.None)).User);
    }

    [Fact]
    public async Task The_last_active_user_admin_cannot_lose_the_permission_or_be_deleted()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        await host.CreateRoleAsync("Viewer");
        var admin = await host.CreateUserAsync("admin", null, "Admin");
        var service = Service(host);

        var update = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(admin.Id, new("admin@example.test", null, ["Viewer"], false), CancellationToken.None));
        Assert.Equal(LastUserAdminMessage, update.Message);
        // Each call is a separate request in production: forget the entities of the rolled-back call.
        host.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(admin.Id, new("admin@example.test", null, ["Admin"], true), CancellationToken.None));
        host.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BusinessRuleException>(() => new PropertyAdmin.Logic.V1.Users.Delete.DeleteHandler(service).HandleAsync(new(admin.Id), CancellationToken.None));

        host.Db.ChangeTracker.Clear();
        var stored = (await service.GetByIdAsync(admin.Id, CancellationToken.None))!;
        Assert.Equal(["Admin"], stored.Roles);
        Assert.False(stored.LockedOut);
    }

    [Fact]
    public async Task Delete_reset_two_factor_and_force_sign_out_report_unknown_users()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        await host.CreateUserAsync("admin", null, "Admin");
        var user = await host.CreateUserAsync("other");
        await host.Users.SetTwoFactorEnabledAsync(user, true);
        var stamp = (await host.Users.FindByIdAsync(user.Id))!.SecurityStamp;
        var service = Service(host);
        var reset = new PropertyAdmin.Logic.V1.Users.ResetTwoFactor.ResetTwoFactorHandler(service);
        var signOut = new PropertyAdmin.Logic.V1.Users.ForceSignOut.ForceSignOutHandler(service);
        var delete = new PropertyAdmin.Logic.V1.Users.Delete.DeleteHandler(service);

        Assert.True((await reset.HandleAsync(new(user.Id), CancellationToken.None)).Found);
        Assert.False(await host.Users.GetTwoFactorEnabledAsync(user));
        Assert.True((await signOut.HandleAsync(new(user.Id), CancellationToken.None)).Found);
        Assert.NotEqual(stamp, (await host.Users.FindByIdAsync(user.Id))!.SecurityStamp);
        Assert.True((await delete.HandleAsync(new(user.Id), CancellationToken.None)).Found);
        Assert.Null(await host.Users.FindByIdAsync(user.Id));

        Assert.False((await reset.HandleAsync(new("unknown"), CancellationToken.None)).Found);
        Assert.False((await signOut.HandleAsync(new("unknown"), CancellationToken.None)).Found);
        Assert.False((await delete.HandleAsync(new("unknown"), CancellationToken.None)).Found);
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through_and_changes_nothing()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        var admin = await host.CreateUserAsync("admin", null, "Admin");
        var service = Service(host);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(new("new", "new@example.test", IdentityTestHost.Password, null, []), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAllAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UpdateAsync(admin.Id, new("x@example.test", null, [], true), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DeleteAsync(admin.Id, cancellation.Token));

        var users = await service.GetAllAsync(CancellationToken.None);
        Assert.Equal("admin", Assert.Single(users).UserName);
        Assert.Equal("admin@example.test", users[0].Email);
    }

    [Fact]
    public void Mappings_copy_every_field()
    {
        IReadOnlyList<string> roles = ["Staff"];
        var model = new UserModel("id", "name", "name@example.test", true, false, 7, roles);

        Assert.Equal(new PropertyAdmin.Api.V1.Users.GetById.Response("id", "name", "name@example.test", true, false, 7, roles),
            PropertyAdmin.Api.V1.Users.GetById.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Users.GetById.Result(model)));
        Assert.Equal(new PropertyAdmin.Api.V1.Users.GetAll.Response("id", "name", "name@example.test", true, false, 7, roles),
            Assert.Single(PropertyAdmin.Api.V1.Users.GetAll.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Users.GetAll.Result([model]))));
        Assert.Equal(new PropertyAdmin.Api.V1.Users.Update.Response("id", "name", "name@example.test", true, false, 7, roles),
            PropertyAdmin.Api.V1.Users.Update.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Users.Update.Result(model)));
        Assert.Equal(new PropertyAdmin.Api.V1.Users.Create.Response("id", "name", "name@example.test", true, false, 7, roles),
            PropertyAdmin.Api.V1.Users.Create.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Users.Create.Result(model)));

        var create = PropertyAdmin.Api.V1.Users.Create.Mapping.ToCommand(new PropertyAdmin.Api.V1.Users.Create.Request("u", "u@example.test", "secret", 3, roles));
        Assert.Equal(new UserCreateModel("u", "u@example.test", "secret", 3, roles), create.User);
        var update = PropertyAdmin.Api.V1.Users.Update.Mapping.ToCommand(new PropertyAdmin.Api.V1.Users.Update.Request("u@example.test", null, roles, true), "id");
        Assert.Equal(new PropertyAdmin.Logic.V1.Users.Update.Command("id", new UserUpdateModel("u@example.test", null, roles, true)), update);
    }

    private const string LastUserAdminMessage = "Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.";

    private static UserService Service(IdentityTestHost host) => new(host.Users, host.Roles, host.Db);
}
