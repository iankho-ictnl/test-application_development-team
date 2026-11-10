using PropertyAdmin.Contracts;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Roles;
using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Tests.V1.Roles;

public sealed class RoleServiceTests
{
    [Fact]
    public async Task Create_then_get_all_returns_sorted_permissions_and_the_user_count()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        var service = Service(host);

        await new PropertyAdmin.Logic.V1.Roles.Create.CreateHandler(service)
            .HandleAsync(new(new RoleSaveModel("Clerk", [Permissions.Reports, Permissions.ReadAll, Permissions.Reports])), CancellationToken.None);
        await host.CreateUserAsync("clerk", null, "Clerk");

        var role = Assert.Single((await new PropertyAdmin.Logic.V1.Roles.GetAll.GetAllHandler(service).HandleAsync(new(), CancellationToken.None)).Roles);
        Assert.Equal("Clerk", role.Name);
        Assert.Equal([Permissions.ReadAll, Permissions.Reports], role.Permissions);
        Assert.Equal(1, role.UserCount);
    }

    [Fact]
    public async Task Create_and_update_refuse_unknown_permissions_and_an_existing_name()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Clerk");
        await host.CreateRoleAsync("Other");
        var service = Service(host);

        Assert.Equal(RoleService.UnknownPermissions, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new("New", ["NoSuchPermission"]), CancellationToken.None))).Message);
        Assert.Equal(RoleService.RoleExists, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(new("Clerk", []), CancellationToken.None))).Message);
        Assert.Equal(RoleService.UnknownPermissions, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync("Clerk", new("Clerk", ["NoSuchPermission"]), CancellationToken.None))).Message);
        Assert.Equal(RoleService.RoleExists, (await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync("Clerk", new("Other", []), CancellationToken.None))).Message);
    }

    [Fact]
    public async Task Update_renames_and_replaces_the_permissions_and_reports_an_unknown_role()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        await host.CreateUserAsync("admin", null, "Admin");
        await host.CreateRoleAsync("Clerk", Permissions.Reports);
        var handler = new PropertyAdmin.Logic.V1.Roles.Update.UpdateHandler(Service(host));

        Assert.True((await handler.HandleAsync(new("Clerk", new RoleSaveModel("Accountant", [Permissions.Invoices, Permissions.ReadAll])), CancellationToken.None)).Found);
        Assert.False((await handler.HandleAsync(new("Unknown", new RoleSaveModel("Unknown", [])), CancellationToken.None)).Found);

        var roles = await Service(host).GetAllAsync(CancellationToken.None);
        Assert.Equal(["Accountant", "Admin"], roles.Select(role => role.Name));
        Assert.Equal([Permissions.Invoices, Permissions.ReadAll], roles[0].Permissions);
    }

    [Fact]
    public async Task Update_refuses_to_take_user_management_from_the_last_active_admin()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Admin", Permissions.Users);
        await host.CreateUserAsync("admin", null, "Admin");
        var service = Service(host);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync("Admin", new("Admin", [Permissions.ReadAll]), CancellationToken.None));

        Assert.Equal("Er moet minstens één actieve gebruiker met gebruikersbeheer overblijven.", error.Message);
        host.Db.ChangeTracker.Clear();
        Assert.Equal([Permissions.Users], Assert.Single(await service.GetAllAsync(CancellationToken.None)).Permissions);
    }

    [Fact]
    public async Task Delete_refuses_a_role_in_use_and_reports_an_unknown_role()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Used");
        await host.CreateRoleAsync("Unused");
        await host.CreateUserAsync("user", null, "Used");
        var service = Service(host);
        var handler = new PropertyAdmin.Logic.V1.Roles.Delete.DeleteHandler(service);

        Assert.Equal(RoleService.RoleInUse, (await Assert.ThrowsAsync<BusinessRuleException>(() => handler.HandleAsync(new("Used"), CancellationToken.None))).Message);
        Assert.True((await handler.HandleAsync(new("Unused"), CancellationToken.None)).Found);
        Assert.False((await handler.HandleAsync(new("Unused"), CancellationToken.None)).Found);
        Assert.Equal("Used", Assert.Single(await service.GetAllAsync(CancellationToken.None)).Name);
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through_and_changes_nothing()
    {
        await using var host = await IdentityTestHost.CreateAsync();
        await host.CreateRoleAsync("Clerk");
        var service = Service(host);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAllAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(new("New", []), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.UpdateAsync("Clerk", new("Renamed", []), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DeleteAsync("Clerk", cancellation.Token));

        Assert.Equal("Clerk", Assert.Single(await service.GetAllAsync(CancellationToken.None)).Name);
    }

    [Fact]
    public void Mappings_copy_every_field()
    {
        IReadOnlyList<string> permissions = [Permissions.Reports];

        Assert.Equal(new PropertyAdmin.Api.V1.Roles.GetAll.Response("Clerk", permissions, 2),
            Assert.Single(PropertyAdmin.Api.V1.Roles.GetAll.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Roles.GetAll.Result([new RoleModel("Clerk", permissions, 2)]))));
        Assert.Equal(new RoleSaveModel("Clerk", permissions),
            PropertyAdmin.Api.V1.Roles.Create.Mapping.ToCommand(new PropertyAdmin.Api.V1.Roles.Create.Request("Clerk", permissions)).Role);
        Assert.Equal(new PropertyAdmin.Logic.V1.Roles.Update.Command("Old", new RoleSaveModel("New", permissions)),
            PropertyAdmin.Api.V1.Roles.Update.Mapping.ToCommand(new PropertyAdmin.Api.V1.Roles.Update.Request("New", permissions), "Old"));
    }

    private static RoleService Service(IdentityTestHost host) => new(host.Roles, host.Db);
}
