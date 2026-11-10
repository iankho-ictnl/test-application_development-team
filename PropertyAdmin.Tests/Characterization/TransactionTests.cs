using System.Net;
using System.Net.Http.Json;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>T-042: current behaviour of api/expenses and api/activities (moved by T-034).</summary>
[Collection(ApiCollection.Name)]
public class ExpensesTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/expenses"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Create_computes_vat_and_get_list_filter_work()
    {
        var admin = await api.As("Admin");
        var assetName = api.Unique("Object");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), assetName);
        var response = await admin.PostAsJsonAsync("/api/expenses", new { assetId, date = "2026-03-01", description = "  Verf  ", amountExclVat = 10.005m, vatRateName = "Hoog", reinvoiceable = false });
        var e = await Expect(response, HttpStatusCode.Created);
        HasShape(e, ExpenseShape);
        Assert.Equal("Verf", e.GetProperty("description").GetString());
        Assert.Equal(assetName, e.GetProperty("assetName").GetString());
        Assert.Equal(10.01m, e.GetProperty("amountExclVat").GetDecimal());
        Assert.Equal(21m, e.GetProperty("vatPercentage").GetDecimal());
        Assert.Equal(2.10m, e.GetProperty("vatAmount").GetDecimal());
        Assert.Equal(12.11m, e.GetProperty("amountInclVat").GetDecimal());
        var id = e.GetProperty("id").GetInt32();

        HasShape(await Expect(await admin.GetAsync($"/api/expenses/{id}"), HttpStatusCode.OK), ExpenseShape);
        Assert.Equal([id], Ids(await Expect(await admin.GetAsync($"/api/expenses?assetId={assetId}&from=2026-03-01&to=2026-03-31"), HttpStatusCode.OK)));
        Assert.Empty(Ids(await Expect(await admin.GetAsync($"/api/expenses?assetId={assetId}&from=2026-04-01"), HttpStatusCode.OK)));
    }

    [Fact]
    public async Task Get_update_delete_unknown_are_404()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await admin.GetAsync("/api/expenses/999999"), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/expenses/999999", new { assetId = 1, date = "2026-01-01", description = "X", amountExclVat = 1m, vatRateName = "Hoog" }), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.DeleteAsync("/api/expenses/999999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_requests_are_400()
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object"));
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/expenses", new { assetId, date = "2026-01-01", amountExclVat = 1m, vatRateName = "Hoog" }));
        await ExpectProblem(await admin.PostAsJsonAsync("/api/expenses", new { assetId = 999999, date = "2026-01-01", description = "X", amountExclVat = 1m, vatRateName = "Hoog" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/expenses", new { assetId, date = "2018-01-01", description = "X", amountExclVat = 1m, vatRateName = "Hoog" }), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Update_and_delete_return_200_and_204()
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object"));
        var id = await admin.CreateExpense(assetId);
        var updated = await Expect(await admin.PutAsJsonAsync($"/api/expenses/{id}", new { assetId, date = "2026-03-05", description = "Nieuw", amountExclVat = 50m, vatRateName = "Laag", reinvoiceable = true }), HttpStatusCode.OK);
        Assert.Equal(9m, updated.GetProperty("vatPercentage").GetDecimal());
        await ExpectStatus(await admin.DeleteAsync($"/api/expenses/{id}"), HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Invoiced_expense_cannot_be_changed_or_deleted()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Object"));
        var id = await admin.CreateExpense(assetId);
        await Expect(await admin.PostAsJsonAsync("/api/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);

        await ExpectProblem(await admin.PutAsJsonAsync($"/api/expenses/{id}", new { assetId, date = "2026-03-01", description = "X", amountExclVat = 1m, vatRateName = "Hoog" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.DeleteAsync($"/api/expenses/{id}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Create_without_permission_is_403() =>
        await ExpectStatus(await (await api.As("Viewer")).PostAsJsonAsync("/api/expenses", new { assetId = 1, date = "2026-01-01", description = "X", amountExclVat = 1m, vatRateName = "Hoog" }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Owner_user_sees_only_own_expenses()
    {
        var admin = await api.As("Admin");
        var own = await admin.CreateOwner(api.Unique("Eigenaar"));
        var ownExpense = await admin.CreateExpense(await admin.CreateAsset(own, api.Unique("Object")));
        var otherExpense = await admin.CreateExpense(await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object")));
        var owner = await api.As("Owner", own);

        Assert.Equal([ownExpense], Ids(await Expect(await owner.GetAsync("/api/expenses"), HttpStatusCode.OK)));
        await ExpectStatus(await owner.GetAsync($"/api/expenses/{otherExpense}"), HttpStatusCode.NotFound);
    }
}

[Collection(ApiCollection.Name)]
public class ActivitiesTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/activities"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Hourly_activity_amount_and_shape()
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object"));
        var response = await admin.PostAsJsonAsync("/api/activities", new { assetId, date = "2026-03-02", kind = 0, description = "Administratie", pricingType = 0, hours = 2.5m, hourlyRate = 40m, fixedPrice = 99m, vatRateName = "Hoog" });
        var a = await Expect(response, HttpStatusCode.Created);
        HasShape(a, ActivityShape);
        Assert.Equal(100m, a.GetProperty("amountExclVat").GetDecimal());
        Assert.Equal(121m, a.GetProperty("amountInclVat").GetDecimal());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, a.GetProperty("fixedPrice").ValueKind);
        var id = a.GetProperty("id").GetInt32();

        HasShape(await Expect(await admin.GetAsync($"/api/activities/{id}"), HttpStatusCode.OK), ActivityShape);
        Assert.Equal([id], Ids(await Expect(await admin.GetAsync($"/api/activities?assetId={assetId}"), HttpStatusCode.OK)));
    }

    [Fact]
    public async Task Get_update_delete_unknown_are_404()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await admin.GetAsync("/api/activities/999999"), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.PutAsJsonAsync("/api/activities/999999", new { assetId = 1, date = "2026-01-01", kind = 0, description = "X", pricingType = 1, fixedPrice = 1m, vatRateName = "Hoog" }), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.DeleteAsync("/api/activities/999999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Missing_price_and_missing_description_are_400()
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object"));
        await ExpectProblem(await admin.PostAsJsonAsync("/api/activities", new { assetId, date = "2026-03-02", kind = 0, description = "X", pricingType = 0, vatRateName = "Hoog" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/activities", new { assetId, date = "2026-03-02", kind = 0, description = "X", pricingType = 1, vatRateName = "Hoog" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/activities", new { assetId, date = "2026-03-02", kind = 0, pricingType = 1, fixedPrice = 1m, vatRateName = "Hoog" }));
    }

    [Fact]
    public async Task Update_delete_and_invoiced_lock()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Object"));
        var body = new { assetId, date = "2026-03-03", kind = 1, description = "Nieuw", pricingType = 1, fixedPrice = 10m, vatRateName = "Nul" };

        var free = await admin.CreateActivity(assetId);
        Assert.Equal(10m, (await Expect(await admin.PutAsJsonAsync($"/api/activities/{free}", body), HttpStatusCode.OK)).GetProperty("amountInclVat").GetDecimal());
        await ExpectStatus(await admin.DeleteAsync($"/api/activities/{free}"), HttpStatusCode.NoContent);

        var invoiced = await admin.CreateActivity(assetId);
        await Expect(await admin.PostAsJsonAsync("/api/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        await ExpectProblem(await admin.PutAsJsonAsync($"/api/activities/{invoiced}", body), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.DeleteAsync($"/api/activities/{invoiced}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Create_without_permission_is_403() =>
        await ExpectStatus(await (await api.As("Finance")).PostAsJsonAsync("/api/activities", new { assetId = 1, date = "2026-01-01", kind = 0, description = "X", pricingType = 1, fixedPrice = 1m, vatRateName = "Hoog" }), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Owner_user_cannot_read_other_owners_activity()
    {
        var admin = await api.As("Admin");
        var own = await admin.CreateOwner(api.Unique("Eigenaar"));
        var other = await admin.CreateActivity(await admin.CreateAsset(await admin.CreateOwner(api.Unique("Eigenaar")), api.Unique("Object")));
        var owner = await api.As("Owner", own);
        await ExpectStatus(await owner.GetAsync($"/api/activities/{other}"), HttpStatusCode.NotFound);
        Assert.Empty(Ids(await Expect(await owner.GetAsync("/api/activities"), HttpStatusCode.OK)));
    }
}
