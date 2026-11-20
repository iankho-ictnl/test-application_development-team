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

        // D-033 (T-161, T-164): without from/to the request is refused at both fields (was 200 with zero totals, Q-044).
        var noPeriod = (await ExpectProblem(await viewer.GetAsync("/api/v1/reports/asset-results"), HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.True(noPeriod.TryGetProperty("from", out _));
        Assert.True(noPeriod.TryGetProperty("to", out _));

        var staff = await api.As("Staff");
        await ExpectStatus(await staff.GetAsync("/api/v1/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Forbidden);
        await ExpectStatus(await staff.GetAsync("/api/v1/reports/owner-overview?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Forbidden);
        await ExpectStatus(await api.Anonymous().GetAsync("/api/v1/reports/asset-results?from=2025-01-01&to=2025-12-31"), HttpStatusCode.Unauthorized);
    }

    public static TheoryData<string, string, string, string, string> InvalidPeriods()
    {
        string[] routes = ["/api/v1/reports/asset-results", "/api/v1/reports/owner-overview", "/api/reports/asset-results", "/api/reports/owner-overview"];
        (string Query, string Field, string En, string Nl)[] cases =
        [
            ("to=2025-12-31", "from", "This field is required.", "Dit veld is verplicht."),
            ("from=2025-01-01", "to", "This field is required.", "Dit veld is verplicht."),
            ("from=2025-13-01&to=2025-12-31", "from", "Enter a valid date as yyyy-MM-dd.", "Vul een geldige datum in als jjjj-mm-dd."),
            ("from=2025-01-01&to=31-12-2025", "to", "Enter a valid date as yyyy-MM-dd.", "Vul een geldige datum in als jjjj-mm-dd."),
            ("from=2025-12-31&to=2025-01-01", "to", "The end date cannot be before the start date.", "De einddatum mag niet voor de begindatum liggen."),
        ];
        var data = new TheoryData<string, string, string, string, string>();
        foreach (var route in routes)
            foreach (var (query, field, en, nl) in cases)
            {
                data.Add(route, query, "en-US", field, en);
                data.Add(route, query, "nl-NL", field, nl);
            }
        return data;
    }

    /// <summary>TC-REQ008-PERIOD-1 (REQ-008, D-033, T-164): from missing, to missing, invalid and reversed give
    /// 400 Problem Details with the error only at its field, in the language of lang, on all four report routes.</summary>
    [Theory]
    [MemberData(nameof(InvalidPeriods))]
    public async Task Invalid_period_gives_400_at_the_field_in_the_request_language(string route, string query, string lang, string field, string message)
    {
        var viewer = await api.As("Viewer");
        var problem = await ExpectProblem(await viewer.GetAsync($"{route}?{query}&lang={lang}"), HttpStatusCode.BadRequest);
        var errors = problem.GetProperty("errors");
        Assert.Equal([message], errors.GetProperty(field).EnumerateArray().Select(item => item.GetString() ?? "").ToArray());
        Assert.Single(errors.EnumerateObject());
    }

    /// <summary>TC-REQ008-PERIOD-2 (REQ-008, D-033): a one-day period (from equals to) is valid.</summary>
    [Fact]
    public async Task One_day_period_is_valid()
    {
        var viewer = await api.As("Viewer");
        await ExpectStatus(await viewer.GetAsync("/api/v1/reports/asset-results?from=2025-05-31&to=2025-05-31"), HttpStatusCode.OK);
        await ExpectStatus(await viewer.GetAsync("/api/v1/reports/owner-overview?from=2025-05-31&to=2025-05-31"), HttpStatusCode.OK);
    }
}
