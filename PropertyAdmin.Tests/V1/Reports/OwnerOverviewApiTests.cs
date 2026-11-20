using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Reports;

/// <summary>
/// REQ-009 (T-173, T19): the owner overview through the real API on both routes. Expected results come from
/// US-9.1 to US-9.5 (REQ-009), the B23 contract (Q-117 names) and T-152 (Q-112 b, Q-113). Test data is fictitious.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class OwnerOverviewApiTests(ApiFactory api)
{
    private const string Period = "from=2026-01-01&to=2026-12-31";

    // TC-REQ009-OVERVIEW-1 (US-9.1, US-9.2, US-9.3, US-9.5)
    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1")]
    public async Task Only_open_invoices_are_listed_and_owners_without_them_are_left_out(string route)
    {
        var admin = await api.As("Admin");
        var withOpen = await admin.CreateOwner(api.Unique("Overzicht open"));
        var onlyPaid = await admin.CreateOwner(api.Unique("Overzicht betaald"));
        var noInvoices = await admin.CreateOwner(api.Unique("Overzicht zonder"));
        await admin.CreateAsset(noInvoices, api.Unique("Pand zonder"));

        var paid = await Invoice(admin, withOpen, "2026-02-02");
        await Pay(admin, paid, "2026-02-10");
        var open = await Invoice(admin, withOpen, "2026-02-20");
        await Pay(admin, await Invoice(admin, onlyPaid, "2026-02-03"), "2026-02-11");

        var viewer = await api.As("Viewer");
        var overview = await Expect(await viewer.GetAsync($"{route}/reports/owner-overview?{Period}"), HttpStatusCode.OK);
        var owner = Row(overview, withOpen);
        var listed = Assert.Single(owner.GetProperty("openInvoices").EnumerateArray().ToList());
        Assert.Equal(open.GetProperty("id").GetInt32(), listed.GetProperty("id").GetInt32());
        Assert.Equal(open.GetProperty("number").GetString(), listed.GetProperty("number").GetString());
        Assert.Equal("2026-02-20", listed.GetProperty("date").GetString());
        Assert.Equal(242m, listed.GetProperty("totalInclVat").GetDecimal());
        Assert.Equal(1, owner.GetProperty("openInvoiceCount").GetInt32());
        Assert.Equal(242m, owner.GetProperty("openInvoiceAmount").GetDecimal());
        Assert.DoesNotContain(overview.EnumerateArray(), item => item.GetProperty("ownerId").GetInt32() == onlyPaid);
        Assert.DoesNotContain(overview.EnumerateArray(), item => item.GetProperty("ownerId").GetInt32() == noInvoices);

        // US-9.3: once paid, the invoice and with it the owner leave the overview.
        await Pay(admin, open, "2026-03-01");
        var after = await Expect(await viewer.GetAsync($"{route}/reports/owner-overview?{Period}"), HttpStatusCode.OK);
        Assert.DoesNotContain(after.EnumerateArray(), item => item.GetProperty("ownerId").GetInt32() == withOpen);
    }

    // TC-REQ009-OVERVIEW-2 (US-9.4, D-046, D-057): due date = invoice date + 14; overdue only when the due date is
    // before today, with a fixed test clock at 2026-03-20 local time.
    [Fact]
    public async Task Due_date_is_invoice_date_plus_14_and_overdue_follows_the_test_clock()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Overzicht vervallen"));
        await Invoice(admin, ownerId, "2026-03-01");
        await Invoice(admin, ownerId, "2026-03-06");
        await Invoice(admin, ownerId, "2026-03-10");

        using var clocked = api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedLocalTime(new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero)));
        }));
        var client = clocked.CreateClient();
        client.DefaultRequestHeaders.Authorization = (await api.As("Viewer")).DefaultRequestHeaders.Authorization;

        var overview = await Expect(await client.GetAsync($"/api/v1/reports/owner-overview?{Period}"), HttpStatusCode.OK);
        var invoices = Row(overview, ownerId).GetProperty("openInvoices").EnumerateArray()
            .ToDictionary(item => item.GetProperty("date").GetString()!);
        Assert.Equal(3, invoices.Count);
        Assert.Equal("2026-03-15", invoices["2026-03-01"].GetProperty("dueDate").GetString());
        Assert.True(invoices["2026-03-01"].GetProperty("overdue").GetBoolean());
        Assert.Equal("2026-03-20", invoices["2026-03-06"].GetProperty("dueDate").GetString());
        Assert.False(invoices["2026-03-06"].GetProperty("overdue").GetBoolean());
        Assert.Equal("2026-03-24", invoices["2026-03-10"].GetProperty("dueDate").GetString());
        Assert.False(invoices["2026-03-10"].GetProperty("overdue").GetBoolean());
    }

    // TC-REQ009-OVERVIEW-3 (T-152, Q-112 b, Q-113): an invoiced line counts under the invoice's owner, an uninvoiced
    // line under the asset's current owner, so one asset counts for two owners after a move.
    [Fact]
    public async Task Lines_count_under_the_owner_at_the_time()
    {
        var admin = await api.As("Admin");
        var first = await admin.CreateOwner(api.Unique("Overzicht eerste"));
        var second = await admin.CreateOwner(api.Unique("Overzicht tweede"));
        var moving = await admin.CreateAsset(first, api.Unique("Pand verhuist"));
        var staying = await admin.CreateAsset(second, api.Unique("Pand blijft"));
        await admin.CreateActivity(moving, "2026-04-01");
        await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId = first, date = "2026-04-01", paymentTermDays = 14 }), HttpStatusCode.Created);
        await admin.CreateActivity(staying, "2026-04-02");
        await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId = second, date = "2026-04-02", paymentTermDays = 14 }), HttpStatusCode.Created);
        await Expect(await admin.PutAsJsonAsync($"/api/v1/assets/{moving}", AssetBody(second, "Pand verhuist")), HttpStatusCode.OK);
        await admin.CreateActivity(moving, "2026-04-05");

        var overview = await Expect(await (await api.As("Viewer")).GetAsync($"/api/v1/reports/owner-overview?{Period}"), HttpStatusCode.OK);
        Assert.Equal(200m, Row(overview, first).GetProperty("income").GetDecimal());
        Assert.Equal(400m, Row(overview, second).GetProperty("income").GetDecimal());
        Assert.Equal(242m, Row(overview, first).GetProperty("openInvoiceAmount").GetDecimal());
        Assert.Equal(242m, Row(overview, second).GetProperty("openInvoiceAmount").GetDecimal());
    }

    /// <summary>An invoice of one fixed-price activity of 200 excl. (242 incl. 21% VAT) on a new asset of the owner.</summary>
    private async Task<JsonElement> Invoice(HttpClient admin, int ownerId, string date)
    {
        var assetId = await admin.CreateAsset(ownerId, api.Unique("Pand"));
        await admin.CreateActivity(assetId, date);
        return await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date, paymentTermDays = 14 }), HttpStatusCode.Created);
    }

    private static async Task Pay(HttpClient admin, JsonElement invoice, string paidDate) =>
        await ExpectStatus(await admin.PostAsJsonAsync($"/api/v1/invoices/{invoice.GetProperty("id").GetInt32()}/pay", new { paidDate }), HttpStatusCode.NoContent);

    private static JsonElement Row(JsonElement overview, int ownerId) =>
        overview.EnumerateArray().Single(item => item.GetProperty("ownerId").GetInt32() == ownerId);

    /// <summary>A test clock at a fixed instant; the local time zone is UTC, so the local date is the UTC date.</summary>
    private sealed class FixedLocalTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
