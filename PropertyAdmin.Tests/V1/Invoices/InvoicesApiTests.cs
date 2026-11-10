using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Invoices;

[Collection(ApiCollection.Name)]
public sealed class InvoicesApiTests(ApiFactory api)
{
    [Fact]
    public async Task Invoices_v1_preserves_shape_scope_rules_and_permissions()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Invoice owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Invoice house"));
        var activityId = await admin.CreateActivity(assetId);
        var expenseId = await admin.CreateExpense(assetId);
        await admin.CreateExpense(assetId, reinvoiceable: false);

        var uninvoiced = await Expect(await admin.GetAsync($"/api/v1/invoices/uninvoiced/{ownerId}"), HttpStatusCode.OK);
        HasShape(uninvoiced, "activities", "expenses");
        Assert.Equal([activityId], Ids(uninvoiced.GetProperty("activities")));
        Assert.Equal([expenseId], Ids(uninvoiced.GetProperty("expenses")));
        HasShape(uninvoiced.GetProperty("activities")[0], ActivityShape);
        HasShape(uninvoiced.GetProperty("expenses")[0], ExpenseShape);

        var created = await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        HasShape(created, InvoiceShape);
        HasShape(created.GetProperty("lines")[0], "description", "amountExclVat", "vatPercentage", "vatAmount", "amountInclVat");
        HasShape(created.GetProperty("vatSummary")[0], "vatPercentage", "base", "vatAmount");
        Assert.Equal(363m, created.GetProperty("totalInclVat").GetDecimal());
        var id = created.GetProperty("id").GetInt32();
        HasShape(await Expect(await admin.GetAsync($"/api/v1/invoices/{id}"), HttpStatusCode.OK), InvoiceShape);
        Assert.Equal([id], Ids(await Expect(await admin.GetAsync($"/api/v1/invoices?ownerId={ownerId}&status=0"), HttpStatusCode.OK)));

        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 400 }));

        var owner = await api.As("Owner", ownerId);
        Assert.Equal([id], Ids(await Expect(await owner.GetAsync("/api/v1/invoices"), HttpStatusCode.OK)));
        var otherOwner = await api.As("Owner", await admin.CreateOwner(api.Unique("Other invoice owner")));
        Assert.Empty(Ids(await Expect(await otherOwner.GetAsync("/api/v1/invoices"), HttpStatusCode.OK)));
        await ExpectStatus(await otherOwner.GetAsync($"/api/v1/invoices/{id}"), HttpStatusCode.NotFound);

        var staff = await api.As("Staff");
        await ExpectStatus(await staff.GetAsync($"/api/v1/invoices/uninvoiced/{ownerId}"), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.PostAsJsonAsync($"/api/v1/invoices/{id}/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.DeleteAsync($"/api/v1/invoices/{id}"), HttpStatusCode.Forbidden);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/invoices"), HttpStatusCode.Unauthorized);

        await ExpectProblem(await admin.PostAsJsonAsync("/api/v1/invoices/999999/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.NotFound);
        await ExpectProblem(await admin.DeleteAsync("/api/v1/invoices/999999"), HttpStatusCode.NotFound);
        await ExpectProblem(await admin.PostAsJsonAsync($"/api/v1/invoices/{id}/pay", new { paidDate = "2026-01-01" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await admin.DeleteAsync($"/api/v1/invoices/{id}"), HttpStatusCode.NoContent);
        var released = await Expect(await admin.GetAsync($"/api/v1/invoices/uninvoiced/{ownerId}"), HttpStatusCode.OK);
        Assert.Equal(1, released.GetProperty("activities").GetArrayLength());

        var again = await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        var againId = again.GetProperty("id").GetInt32();
        await ExpectStatus(await admin.PostAsJsonAsync($"/api/v1/invoices/{againId}/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.NoContent);
        var paid = await Expect(await admin.GetAsync($"/api/v1/invoices/{againId}"), HttpStatusCode.OK);
        Assert.Equal(1, paid.GetProperty("status").GetInt32());
        await ExpectProblem(await admin.DeleteAsync($"/api/v1/invoices/{againId}"), HttpStatusCode.BadRequest, DomainTitle);
    }
}
