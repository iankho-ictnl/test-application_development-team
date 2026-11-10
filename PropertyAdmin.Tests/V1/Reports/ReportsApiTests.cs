using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Reports;

[Collection(ApiCollection.Name)]
public sealed class ReportsApiTests(ApiFactory api)
{
    [Fact]
    public async Task Reports_v1_preserves_shape_totals_and_permissions()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Report owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Report house"));
        await admin.CreateActivity(assetId, "2025-06-01");
        await admin.CreateExpense(assetId, date: "2025-06-02");
        await admin.CreateExpense(assetId, reinvoiceable: false, date: "2024-06-02");
        await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2025-06-30", paymentTermDays = 30 }), HttpStatusCode.Created);

        var viewer = await api.As("Viewer");
        var results = await Expect(await viewer.GetAsync("/api/v1/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.OK);
        var row = results.EnumerateArray().Single(item => item.GetProperty("assetId").GetInt32() == assetId);
        HasShape(row, "assetId", "assetName", "ownerName", "income", "expenses", "result");
        Assert.Equal(200m, row.GetProperty("income").GetDecimal());
        Assert.Equal(100m, row.GetProperty("expenses").GetDecimal());
        Assert.Equal(100m, row.GetProperty("result").GetDecimal());

        var overview = await Expect(await viewer.GetAsync("/api/v1/reports/owner-overview?from=2025-01-01&to=2025-12-31"), HttpStatusCode.OK);
        var owner = overview.EnumerateArray().Single(item => item.GetProperty("ownerId").GetInt32() == ownerId);
        HasShape(owner, "ownerId", "ownerName", "assetCount", "income", "expenses", "openInvoiceCount", "openInvoiceAmount");
        Assert.Equal(1, owner.GetProperty("openInvoiceCount").GetInt32());
        Assert.Equal(363m, owner.GetProperty("openInvoiceAmount").GetDecimal());

        // Current behaviour kept (Q-044): without from/to every total is 0.
        var noPeriod = await Expect(await viewer.GetAsync("/api/v1/reports/asset-results"), HttpStatusCode.OK);
        Assert.All(noPeriod.EnumerateArray(), item => Assert.Equal(0m, item.GetProperty("income").GetDecimal()));

        var staff = await api.As("Staff");
        await ExpectStatus(await staff.GetAsync("/api/v1/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.GetAsync("/api/v1/reports/owner-overview?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Forbidden);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Unauthorized);
    }
}
