using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>T-042: current behaviour of api/invoices and api/reports (moved by T-035).</summary>
[Collection(ApiCollection.Name)]
public class InvoicesTests(ApiFactory api)
{
    private async Task<(HttpClient admin, int ownerId, int invoiceId, JsonElement invoice)> InvoicedOwner()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Object"));
        await admin.CreateActivity(assetId);
        await admin.CreateExpense(assetId);
        await admin.CreateExpense(assetId, reinvoiceable: false);
        var invoice = await Expect(await admin.PostAsJsonAsync("/api/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        return (admin, ownerId, invoice.GetProperty("id").GetInt32(), invoice);
    }

    [Fact]
    public async Task Anonymous_list_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/invoices"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Uninvoiced_lists_activities_and_reinvoiceable_expenses_only()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Eigenaar"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Object"));
        var activity = await admin.CreateActivity(assetId);
        var expense = await admin.CreateExpense(assetId);
        await admin.CreateExpense(assetId, reinvoiceable: false);

        var json = await Expect(await admin.GetAsync($"/api/invoices/uninvoiced/{ownerId}"), HttpStatusCode.OK);
        HasShape(json, "activities", "expenses");
        Assert.Equal([activity], Ids(json.GetProperty("activities")));
        Assert.Equal([expense], Ids(json.GetProperty("expenses")));
    }

    [Fact]
    public async Task Create_returns_201_with_number_lines_and_totals()
    {
        var (_, ownerId, _, invoice) = await InvoicedOwner();
        HasShape(invoice, InvoiceShape);
        Assert.Matches(@"^2026-\d{4}$", invoice.GetProperty("number").GetString());
        Assert.Equal(ownerId, invoice.GetProperty("ownerId").GetInt32());
        Assert.Equal("2026-04-14", invoice.GetProperty("dueDate").GetString());
        Assert.Equal(0, invoice.GetProperty("status").GetInt32());
        Assert.Equal(2, invoice.GetProperty("lines").GetArrayLength());
        Assert.Equal(300m, invoice.GetProperty("totalExclVat").GetDecimal());
        Assert.Equal(63m, invoice.GetProperty("totalVat").GetDecimal());
        Assert.Equal(363m, invoice.GetProperty("totalInclVat").GetDecimal());
        var line = invoice.GetProperty("lines")[0];
        HasShape(line, "description", "amountExclVat", "vatPercentage", "vatAmount", "amountInclVat");
        HasShape(invoice.GetProperty("vatSummary")[0], "vatPercentage", "base", "vatAmount");
    }

    [Fact]
    public async Task Create_without_lines_or_owner_is_400()
    {
        var admin = await api.As("Admin");
        var empty = await admin.CreateOwner(api.Unique("Eigenaar"));
        await ExpectProblem(await admin.PostAsJsonAsync("/api/invoices", new { ownerId = empty, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/invoices", new { ownerId = 999999, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectValidationProblem(await admin.PostAsJsonAsync("/api/invoices", new { ownerId = empty, date = "2026-03-31", paymentTermDays = 400 }));
    }

    [Fact]
    public async Task Get_list_and_pdf()
    {
        var (admin, ownerId, id, _) = await InvoicedOwner();
        HasShape(await Expect(await admin.GetAsync($"/api/invoices/{id}"), HttpStatusCode.OK), InvoiceShape);
        Assert.Equal([id], Ids(await Expect(await admin.GetAsync($"/api/invoices?ownerId={ownerId}&status=0"), HttpStatusCode.OK)));

        var pdf = await admin.GetAsync($"/api/invoices/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("Factuur-2026-", pdf.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
    }

    [Fact]
    public async Task Unknown_invoice_is_404_everywhere()
    {
        var admin = await api.As("Admin");
        await ExpectStatus(await admin.GetAsync("/api/invoices/999999"), HttpStatusCode.NotFound);
        await ExpectStatus(await admin.GetAsync("/api/invoices/999999/pdf"), HttpStatusCode.NotFound);
        await ExpectProblem(await admin.PostAsJsonAsync("/api/invoices/999999/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.NotFound);
        await ExpectProblem(await admin.DeleteAsync("/api/invoices/999999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Pay_returns_204_then_paid_invoice_cannot_be_paid_or_deleted()
    {
        var (admin, _, id, _) = await InvoicedOwner();
        await ExpectProblem(await admin.PostAsJsonAsync($"/api/invoices/{id}/pay", new { paidDate = "2026-01-01" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectStatus(await admin.PostAsJsonAsync($"/api/invoices/{id}/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.NoContent);
        var paid = await Expect(await admin.GetAsync($"/api/invoices/{id}"), HttpStatusCode.OK);
        Assert.Equal(1, paid.GetProperty("status").GetInt32());
        Assert.Equal("2026-04-01", paid.GetProperty("paidDate").GetString());
        await ExpectProblem(await admin.PostAsJsonAsync($"/api/invoices/{id}/pay", new { paidDate = "2026-04-02" }), HttpStatusCode.BadRequest, DomainTitle);
        await ExpectProblem(await admin.DeleteAsync($"/api/invoices/{id}"), HttpStatusCode.BadRequest, DomainTitle);
    }

    [Fact]
    public async Task Delete_open_invoice_returns_204_and_releases_lines()
    {
        var (admin, ownerId, id, _) = await InvoicedOwner();
        await ExpectStatus(await admin.DeleteAsync($"/api/invoices/{id}"), HttpStatusCode.NoContent);
        var json = await Expect(await admin.GetAsync($"/api/invoices/uninvoiced/{ownerId}"), HttpStatusCode.OK);
        Assert.Equal(1, json.GetProperty("activities").GetArrayLength());
        Assert.Equal(1, json.GetProperty("expenses").GetArrayLength());
    }

    [Fact]
    public async Task Without_invoices_permission_is_403()
    {
        var staff = await api.As("Staff");
        await ExpectStatus(await staff.GetAsync("/api/invoices/uninvoiced/1"), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.PostAsJsonAsync("/api/invoices", new { ownerId = 1, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.PostAsJsonAsync("/api/invoices/1/pay", new { paidDate = "2026-04-01" }), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.DeleteAsync("/api/invoices/1"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_user_cannot_read_other_owners_invoice_or_pdf()
    {
        var (_, _, id, _) = await InvoicedOwner();
        var admin = await api.As("Admin");
        var owner = await api.As("Owner", await admin.CreateOwner(api.Unique("Eigenaar")));
        await ExpectStatus(await owner.GetAsync($"/api/invoices/{id}"), HttpStatusCode.NotFound);
        await ExpectStatus(await owner.GetAsync($"/api/invoices/{id}/pdf"), HttpStatusCode.NotFound);
        Assert.Empty(Ids(await Expect(await owner.GetAsync("/api/invoices"), HttpStatusCode.OK)));
    }
}

[Collection(ApiCollection.Name)]
public class ReportsTests(ApiFactory api)
{
    [Fact]
    public async Task Anonymous_is_401() =>
        await ExpectStatus(await api.Anonymous().GetAsync("/api/reports/asset-results?from=2026-01-01&to=2026-12-31"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Without_reports_permission_is_403()
    {
        var staff = await api.As("Staff");
        await ExpectStatus(await staff.GetAsync("/api/reports/asset-results?from=2026-01-01&to=2026-12-31"), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.GetAsync("/api/reports/owner-overview?from=2026-01-01&to=2026-12-31"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Missing_period_currently_returns_200_with_zero_totals()
    {
        // Current behaviour (Q-044): without from/to both default to 0001-01-01, so every total is 0.
        var json = await Expect(await (await api.As("Viewer")).GetAsync("/api/reports/asset-results"), HttpStatusCode.OK);
        Assert.All(json.EnumerateArray(), r => Assert.Equal(0m, r.GetProperty("income").GetDecimal()));
    }

    [Fact]
    public async Task Asset_results_and_owner_overview_totals()
    {
        var admin = await api.As("Admin");
        var ownerName = api.Unique("Eigenaar");
        var ownerId = await admin.CreateOwner(ownerName);
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Object"));
        await admin.CreateActivity(assetId, "2025-06-01");
        await admin.CreateExpense(assetId, date: "2025-06-02");
        await admin.CreateExpense(assetId, reinvoiceable: false, date: "2024-06-02");
        await Expect(await admin.PostAsJsonAsync("/api/invoices", new { ownerId, date = "2025-06-30", paymentTermDays = 30 }), HttpStatusCode.Created);

        var viewer = await api.As("Viewer");
        var results = await Expect(await viewer.GetAsync("/api/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.OK);
        var row = results.EnumerateArray().Single(r => r.GetProperty("assetId").GetInt32() == assetId);
        HasShape(row, "assetId", "assetName", "ownerName", "income", "expenses", "result");
        Assert.Equal(200m, row.GetProperty("income").GetDecimal());
        Assert.Equal(100m, row.GetProperty("expenses").GetDecimal());
        Assert.Equal(100m, row.GetProperty("result").GetDecimal());

        var overview = await Expect(await viewer.GetAsync("/api/reports/owner-overview?from=2025-01-01&to=2025-12-31"), HttpStatusCode.OK);
        var o = overview.EnumerateArray().Single(r => r.GetProperty("ownerId").GetInt32() == ownerId);
        HasShape(o, "ownerId", "ownerName", "assetCount", "income", "expenses", "openInvoiceCount", "openInvoiceAmount");
        Assert.Equal(1, o.GetProperty("assetCount").GetInt32());
        Assert.Equal(1, o.GetProperty("openInvoiceCount").GetInt32());
        Assert.Equal(363m, o.GetProperty("openInvoiceAmount").GetDecimal());
    }
}
