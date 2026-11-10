using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1;

[Collection(ApiCollection.Name)]
public sealed class MasterDataApiTests(ApiFactory api)
{
    [Fact]
    public async Task Owners_v1_preserves_shape_and_owner_read_scope()
    {
        var admin = await api.As("Admin");
        var own = await Expect(await admin.PostAsJsonAsync("/api/v1/owners", new { name = "Owner One" }), HttpStatusCode.Created);
        var other = await Expect(await admin.PostAsJsonAsync("/api/v1/owners", new { name = "Owner Two" }), HttpStatusCode.Created);
        HasShape(own, OwnerShape);
        var owner = await api.As("Owner", own.GetProperty("id").GetInt32());

        Assert.Equal([own.GetProperty("id").GetInt32()], Ids(await Expect(await owner.GetAsync("/api/v1/owners"), HttpStatusCode.OK)));
        await ExpectStatus(await owner.GetAsync($"/api/v1/owners/{other.GetProperty("id").GetInt32()}"), HttpStatusCode.NotFound);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/owners"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Assets_v1_enforces_owner_scope_and_returns_owner_name()
    {
        var admin = await api.As("Admin");
        var owner = await Expect(await admin.PostAsJsonAsync("/api/v1/owners", new { name = "Asset Owner" }), HttpStatusCode.Created);
        var otherOwner = await Expect(await admin.PostAsJsonAsync("/api/v1/owners", new { name = "Other Owner" }), HttpStatusCode.Created);
        var asset = await Expect(await admin.PostAsJsonAsync("/api/v1/assets", new
        {
            ownerId = owner.GetProperty("id").GetInt32(),
            type = 0,
            name = "House",
            address = "Road 1",
            postalCode = "1000 AA",
            city = "City",
        }), HttpStatusCode.Created);
        HasShape(asset, AssetShape);
        Assert.Equal("Asset Owner", asset.GetProperty("ownerName").GetString());

        var ownerClient = await api.As("Owner", owner.GetProperty("id").GetInt32());
        Assert.Equal([asset.GetProperty("id").GetInt32()], Ids(await Expect(await ownerClient.GetAsync("/api/v1/assets"), HttpStatusCode.OK)));
        Assert.Empty(Ids(await Expect(await ownerClient.GetAsync($"/api/v1/assets?ownerId={otherOwner.GetProperty("id").GetInt32()}"), HttpStatusCode.OK)));
    }

    [Fact]
    public async Task Vat_rates_v1_keeps_reads_authenticated_and_writes_permissioned()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/vatrates"), HttpStatusCode.Unauthorized);
        var finance = await api.As("Finance");
        var created = await Expect(await finance.PostAsJsonAsync("/api/v1/vatrates", new
        {
            name = api.Unique("Rate"),
            percentage = 6m,
            validFrom = "2026-01-01",
            validTo = "2026-12-31",
        }), HttpStatusCode.Created);
        HasShape(created, VatRateShape);
        var list = await Expect(await admin.GetAsync("/api/v1/vatrates"), HttpStatusCode.OK);
        Assert.Contains(created.GetProperty("id").GetInt32(), Ids(list));
        await ExpectStatus(await (await api.As("Staff")).PostAsJsonAsync("/api/v1/vatrates", new
        {
            name = api.Unique("DeniedRate"),
            percentage = 1m,
            validFrom = "2026-01-01",
        }), HttpStatusCode.Forbidden);
    }
}