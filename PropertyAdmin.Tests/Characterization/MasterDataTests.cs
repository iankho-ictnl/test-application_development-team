using System.Net;
using System.Net.Http.Json;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>T-042: current behaviour of api/owners, api/assets and api/vatrates (moved by T-033).</summary>
[Collection(ApiCollection.Name)]
public class OwnersTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/owners"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task List_and_get_return_owner_shape()
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Eigenaar"));

        var list = await Expect(await admin.GetAsync("/api/owners"), HttpStatusCode.OK);
        Assert.Contains(id, Ids(list));
        var one = await Expect(await admin.GetAsync($"/api/owners/{id}"), HttpStatusCode.OK);
        HasShape(one, OwnerShape);
        Assert.Equal("Teststad", one.GetProperty("city").GetString());
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        await ExpectStatus(await (await api.As("Admin")).GetAsync("/api/owners/999999"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_returns_201_with_location_and_trimmed_name()
    {
        var admin = await api.As("Admin");
        var name = api.Unique("Eigenaar");
        var response = await admin.PostAsJsonAsync("/api/owners", new { name = $"  {name}  " });
        var json = await Expect(response, HttpStatusCode.Created);
        HasShape(json, OwnerShape);
        Assert.Equal(name, json.GetProperty("name").GetString());
        Assert.EndsWith($"/api/owners/{json.GetProperty("id").GetInt32()}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_without_name_is_validation_problem() =>
        await ExpectValidationProblem(await (await api.As("Admin")).PostAsJsonAsync("/api/owners", new { email = "geen-naam@example.test" }));

    [Fact]
    public async Task Create_without_permission_is_403() =>
        await ExpectStatus(await (await api.As("Staff")).PostAsJsonAsync("/api/owners", new { name = "X" }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Update_returns_200_and_unknown_is_404()
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Eigenaar"));
        var json = await Expect(await admin.PutAsJsonAsync($"/api/owners/{id}", new { name = "Gewijzigd", city = "Andere stad" }), HttpStatusCode.OK);
        HasShape(json, OwnerShape);
        Assert.Equal("Andere stad", json.GetProperty("city").GetString());
        await ExpectStatus(await admin.PutAsJsonAsync("/api/owners/999999", new { name = "X" }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_returns_204_404_and_400_when_owner_has_assets()
    {
        var admin = await api.As("Admin");
        var free = await admin.CreateOwner(api.Unique("Eigenaar"));
        await ExpectStatus(await admin.DeleteAsync($"/api/owners/{free}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/owners/{free}"), HttpStatusCode.NotFound);

        var busy = await admin.CreateOwner(api.Unique("Eigenaar"));
        await admin.CreateAsset(busy, api.Unique("Object"));
        await ExpectProblem(await admin.DeleteAsync($"/api/owners/{busy}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Owner_user_sees_only_own_owner()
    {
        var admin = await api.As("Admin");
        var own = await admin.CreateOwner(api.Unique("Eigenaar"));
        var other = await admin.CreateOwner(api.Unique("Eigenaar"));
        var owner = await api.As("Owner", own);

        var list = await Expect(await owner.GetAsync("/api/owners"), HttpStatusCode.OK);
        Assert.Equal([own], Ids(list));
        await ExpectStatus(await owner.GetAsync($"/api/owners/{other}"), HttpStatusCode.NotFound);
    }
}

[Collection(ApiCollection.Name)]
public class AssetsTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/assets"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Create_get_and_list_filtered_by_owner()
    {
        var admin = await api.As("Admin");
        var ownerName = api.Unique("Eigenaar");
        var ownerId = await admin.CreateOwner(ownerName);
        var response = await admin.PostAsJsonAsync("/api/assets", new { ownerId, type = 1, name = "  Woning A  ", address = "Weg 1", postalCode = "1000 AA", city = "Stad" });
        var created = await Expect(response, HttpStatusCode.Created);
        HasShape(created, AssetShape);
        Assert.Equal("Woning A", created.GetProperty("name").GetString());
        Assert.Equal(ownerName, created.GetProperty("ownerName").GetString());
        Assert.Equal(1, created.GetProperty("type").GetInt32());
        var id = created.GetProperty("id").GetInt32();

        HasShape(await Expect(await admin.GetAsync($"/api/assets/{id}"), HttpStatusCode.OK), AssetShape);
        var list = await Expect(await admin.GetAsync($"/api/assets?ownerId={ownerId}"), HttpStatusCode.OK);
        Assert.Equal([id], Ids(list));
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        await ExpectStatus(await (await api.As("Admin")).GetAsync("/api/assets/999999"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_missing_fields_is_validation_problem() =>
        await ExpectValidationProblem(await (await api.As("Admin")).PostAsJsonAsync("/api/assets", new { ownerId = 0, type = 0 }));

    [Fact]
    public async Task Create_for_unknown_owner_is_400_domain_problem() =>
        await ExpectProblem(await (await api.As("Admin")).PostAsJsonAsync("/api/assets", new { ownerId = 999999, type = 0, name = "X", address = "Y", postalCode = "Z", city = "Q" }), HttpStatusCode.BadRequest, DomainTitle);

    [Fact]
    public async Task Create_without_permission_is_403() =>
        await ExpectStatus(await (await api.As("Staff")).PostAsJsonAsync("/api/assets", new { ownerId = 1, type = 0, name = "X", address = "Y", postalCode = "Z", city = "Q" }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Update_returns_200_404_and_400_when_owner_changes_with_uninvoiced_lines()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var otherOwner = await admin.CreateOwner(api.Unique("Eigenaar"));
        var id = await admin.CreateAsset(ownerId, api.Unique("Object"));
        var body = new { ownerId, type = 0, name = "Nieuw", address = "Weg", postalCode = "1", city = "Stad" };
        HasShape(await Expect(await admin.PutAsJsonAsync($"/api/assets/{id}", body), HttpStatusCode.OK), AssetShape);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/assets/999999", body), HttpStatusCode.NotFound);

        await admin.CreateActivity(id);
        await ExpectProblem(await admin.PutAsJsonAsync($"/api/assets/{id}", body with { ownerId = otherOwner }), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Delete_returns_204_404_and_400_when_asset_has_expenses()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var free = await admin.CreateAsset(ownerId, api.Unique("Object"));
        await ExpectStatus(await admin.DeleteAsync($"/api/assets/{free}"), HttpStatusCode.NoContent);
        await ExpectStatus(await admin.DeleteAsync($"/api/assets/{free}"), HttpStatusCode.NotFound);

        var busy = await admin.CreateAsset(ownerId, api.Unique("Object"));
        await admin.CreateExpense(busy);
        await ExpectProblem(await admin.DeleteAsync($"/api/assets/{busy}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Owner_user_sees_only_own_assets()
    {
        var admin = await api.As("Admin");
        var own = await admin.CreateOwner(api.Unique("Eigenaar"));
        var other = await admin.CreateOwner(api.Unique("Eigenaar"));
        var ownAsset = await admin.CreateAsset(own, api.Unique("Object"));
        var otherAsset = await admin.CreateAsset(other, api.Unique("Object"));
        var owner = await api.As("Owner", own);

        Assert.Equal([ownAsset], Ids(await Expect(await owner.GetAsync("/api/assets"), HttpStatusCode.OK)));
        await ExpectStatus(await owner.GetAsync($"/api/assets/{otherAsset}"), HttpStatusCode.NotFound);
    }
}

[Collection(ApiCollection.Name)]
public class VatRatesTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/vatrates"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Any_signed_in_user_lists_the_seeded_rates()
    {
        var owner = await api.As("Owner", await (await api.As("Admin")).CreateOwner(api.Unique("Eigenaar")));
        var list = await Expect(await owner.GetAsync("/api/vatrates"), HttpStatusCode.OK);
        var hoog = list.EnumerateArray().First(v => v.GetProperty("name").GetString() == "Hoog");
        HasShape(hoog, VatRateShape);
        Assert.Equal(21m, hoog.GetProperty("percentage").GetDecimal());
        Assert.Equal("2019-01-01", hoog.GetProperty("validFrom").GetString());
    }

    [Fact]
    public async Task Create_get_update_delete()
    {
        var finance = await api.As("Finance");
        var name = api.Unique("Tarief");
        var response = await finance.PostAsJsonAsync("/api/vatrates", new { name, percentage = 6m, validFrom = "2020-01-01", validTo = "2020-12-31" });
        var created = await Expect(response, HttpStatusCode.Created);
        HasShape(created, VatRateShape);
        var id = created.GetProperty("id").GetInt32();

        HasShape(await Expect(await finance.GetAsync($"/api/vatrates/{id}"), HttpStatusCode.OK), VatRateShape);
        var updated = await Expect(await finance.PutAsJsonAsync($"/api/vatrates/{id}", new { name, percentage = 7m, validFrom = "2020-01-01", validTo = (string?)null }), HttpStatusCode.OK);
        Assert.Equal(7m, updated.GetProperty("percentage").GetDecimal());
        await ExpectStatus(await finance.DeleteAsync($"/api/vatrates/{id}"), HttpStatusCode.NoContent);
        await ExpectStatus(await finance.DeleteAsync($"/api/vatrates/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_and_update_unknown_are_404()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await admin.GetAsync("/api/vatrates/999999"), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/vatrates/999999", new { name = "X", percentage = 1m, validFrom = "2020-01-01" }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Overlap_and_end_before_start_are_400_domain_problems()
    {
        var admin = await api.As("Admin");
        await ExpectProblem(await admin.PostAsJsonAsync("/api/vatrates", new { name = "Hoog", percentage = 22m, validFrom = "2025-01-01" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/vatrates", new { name = api.Unique("Tarief"), percentage = 5m, validFrom = "2025-01-01", validTo = "2024-01-01" }), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Percentage_out_of_range_is_validation_problem() =>
        await ExpectValidationProblem(await (await api.As("Admin")).PostAsJsonAsync("/api/vatrates", new { name = "X", percentage = 150m, validFrom = "2025-01-01" }));

    [Fact]
    public async Task Create_without_permission_is_403() =>
        await ExpectStatus(await (await api.As("Staff")).PostAsJsonAsync("/api/vatrates", new { name = "X", percentage = 1m, validFrom = "2025-01-01" }), HttpStatusCode.Forbidden);
}
