using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Expenses;

[Collection(ApiCollection.Name)]
public sealed class ExpensesApiTests(ApiFactory api)
{
    [Fact]
    public async Task Expenses_v1_preserves_shape_scope_and_permissions()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Expense owner"));
        var otherOwnerId = await admin.CreateOwner(api.Unique("Other expense owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Expense house"));
        var otherAssetId = await admin.CreateAsset(otherOwnerId, api.Unique("Other expense house"));
        var expense = new { assetId, date = "2026-03-01", description = "Lamp", category = "Onderhoud", amountExclVat = 100.00m, vatRateName = "Hoog", reinvoiceable = true };

        var created = await Expect(await admin.PostAsJsonAsync("/api/v1/expenses", expense), HttpStatusCode.Created);
        HasShape(created, ExpenseShape);
        var id = created.GetProperty("id").GetInt32();
        var other = await Expect(await admin.PostAsJsonAsync("/api/v1/expenses", expense with { assetId = otherAssetId }), HttpStatusCode.Created);
        var updated = await Expect(await admin.PutAsJsonAsync($"/api/v1/expenses/{id}", expense with { description = "Lamp vervangen" }), HttpStatusCode.OK);
        Assert.Equal("Lamp vervangen", updated.GetProperty("description").GetString());

        var owner = await api.As("Owner", ownerId);
        Assert.Equal([id], Ids(await Expect(await owner.GetAsync("/api/v1/expenses"), HttpStatusCode.OK)));
        HasShape(await Expect(await owner.GetAsync($"/api/v1/expenses/{id}"), HttpStatusCode.OK), ExpenseShape);
        await ExpectStatus(await owner.GetAsync($"/api/v1/expenses/{other.GetProperty("id").GetInt32()}"), HttpStatusCode.NotFound);
        await ExpectStatus(await owner.PostAsJsonAsync("/api/v1/expenses", expense), HttpStatusCode.Forbidden);
        await ExpectStatus(await (await api.As("Viewer")).DeleteAsync($"/api/v1/expenses/{id}"), HttpStatusCode.Forbidden);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/expenses"), HttpStatusCode.Unauthorized);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/expenses", expense with { vatRateName = "Onbekend" }), HttpStatusCode.BadRequest, DomainTitle);

        await ExpectStatus(await (await api.As("Staff")).DeleteAsync($"/api/v1/expenses/{id}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.GetAsync($"/api/v1/expenses/{id}"), HttpStatusCode.NotFound);
    }
}
