using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Users;

[Collection(ApiCollection.Name)]
public sealed class UsersApiTests(ApiFactory api)
{
    [Fact]
    public async Task Users_and_roles_v1_refuse_anonymous_callers_and_callers_without_the_users_permission()
    {
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/users"), HttpStatusCode.Unauthorized);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/roles"), HttpStatusCode.Unauthorized);
        var manager = await api.As("Manager");
        await ExpectStatus(await manager.GetAsync("/api/v1/users"), HttpStatusCode.Forbidden);
        await ExpectStatus(await manager.PostAsync("/api/v1/users/unknown-id/sign-out", null), HttpStatusCode.Forbidden);
        await ExpectStatus(await manager.GetAsync("/api/v1/roles"), HttpStatusCode.Forbidden);
        await ExpectStatus(await manager.DeleteAsync("/api/v1/roles/Staff"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_v1_create_get_update_and_delete()
    {
        var admin = await api.As("Admin");
        var userName = api.Unique("v1user");
        var created = await Expect(await admin.PostAsJsonAsync("/api/v1/users", new { userName, email = $"{userName}@example.test", password = ApiFactory.Password, roles = new[] { "Staff" } }), HttpStatusCode.Created);
        var id = created.GetProperty("id").GetString();

        var found = await Expect(await admin.GetAsync($"/api/v1/users/{id}"), HttpStatusCode.OK);
        Assert.Equal(userName, found.GetProperty("userName").GetString());
        var updated = await Expect(await admin.PutAsJsonAsync($"/api/v1/users/{id}", new { email = $"{userName}@example.test", roles = new[] { "Viewer" }, lockedOut = false }), HttpStatusCode.OK);
        Assert.Equal(["Viewer"], updated.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/users", new { userName = api.Unique("v1user"), email = "v1@example.test", password = ApiFactory.Password, roles = new[] { "NoSuchRole" } }), HttpStatusCode.BadRequest);

        await ExpectStatus(await admin.PostAsync($"/api/v1/users/{id}/reset-2fa", null), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/v1/users/{id}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.GetAsync($"/api/v1/users/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Roles_v1_create_list_update_and_delete()
    {
        var admin = await api.As("Admin");
        var name = api.Unique("V1Rol");
        await ExpectStatus(await admin.PostAsJsonAsync("/api/v1/roles", new { name, permissions = new[] { "Reports" } }), HttpStatusCode.NoContent);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/roles", new { name, permissions = new[] { "NoSuchPermission" } }), HttpStatusCode.BadRequest);

        var roles = await Expect(await admin.GetAsync("/api/v1/roles"), HttpStatusCode.OK);
        var role = roles.EnumerateArray().Single(r => r.GetProperty("name").GetString() == name);
        Assert.Equal(0, role.GetProperty("userCount").GetInt32());

        await ExpectStatus(await admin.PutAsJsonAsync($"/api/v1/roles/{name}", new { name, permissions = new[] { "ReadAll" } }), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/v1/roles/{name}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/v1/roles/{name}"), HttpStatusCode.NotFound);
    }
}
