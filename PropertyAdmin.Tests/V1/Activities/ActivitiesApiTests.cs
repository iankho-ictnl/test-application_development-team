using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Activities;

[Collection(ApiCollection.Name)]
public sealed class ActivitiesApiTests(ApiFactory api)
{
    [Fact]
    public async Task Activities_v1_preserves_shape_scope_and_permissions()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Activity owner"));
        var otherOwnerId = await admin.CreateOwner(api.Unique("Other activity owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Activity house"));
        var otherAssetId = await admin.CreateAsset(otherOwnerId, api.Unique("Other activity house"));
        var activity = new { assetId, date = "2026-03-02", kind = 0, description = "Administratie", pricingType = 0, hours = (decimal?)2.5m, hourlyRate = (decimal?)40m, fixedPrice = (decimal?)99m, vatRateName = "Hoog" };

        var created = await Expect(await admin.PostAsJsonAsync("/api/v1/activities", activity), HttpStatusCode.Created);
        HasShape(created, ActivityShape);
        Assert.Equal(100m, created.GetProperty("amountExclVat").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, created.GetProperty("fixedPrice").ValueKind);
        var id = created.GetProperty("id").GetInt32();
        var other = await Expect(await admin.PostAsJsonAsync("/api/v1/activities", activity with { assetId = otherAssetId }), HttpStatusCode.Created);
        var updated = await Expect(await admin.PutAsJsonAsync($"/api/v1/activities/{id}", activity with { pricingType = 1, hours = null, hourlyRate = null, fixedPrice = 10m }), HttpStatusCode.OK);
        Assert.Equal(10m, updated.GetProperty("amountExclVat").GetDecimal());

        var owner = await api.As("Owner", ownerId);
        Assert.Equal([id], Ids(await Expect(await owner.GetAsync("/api/v1/activities"), HttpStatusCode.OK)));
        HasShape(await Expect(await owner.GetAsync($"/api/v1/activities/{id}"), HttpStatusCode.OK), ActivityShape);
        await ExpectStatus(await owner.GetAsync($"/api/v1/activities/{other.GetProperty("id").GetInt32()}"), HttpStatusCode.NotFound);
        await ExpectStatus(await owner.PostAsJsonAsync("/api/v1/activities", activity), HttpStatusCode.Forbidden);
        await ExpectStatus(await (await api.As("Finance")).PostAsJsonAsync("/api/v1/activities", activity), HttpStatusCode.Forbidden);
        await ExpectStatus(await (await api.As("Viewer")).DeleteAsync($"/api/v1/activities/{id}"), HttpStatusCode.Forbidden);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/activities"), HttpStatusCode.Unauthorized);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/activities", activity with { hours = null }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/activities", activity with { vatRateName = "Onbekend" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/v1/activities/999999", activity), HttpStatusCode.NotFound);

        await ExpectStatus(await (await api.As("Staff")).DeleteAsync($"/api/v1/activities/{id}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.GetAsync($"/api/v1/activities/{id}"), HttpStatusCode.NotFound);
    }
}
