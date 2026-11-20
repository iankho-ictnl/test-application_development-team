using System.Net;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>TC-12: each screen shows the key content from the API (signed in as Admin, own test data, nl-NL formatting).</summary>
[Collection(FrontendCollection.Name)]
[Trait("TestCase", "TC-12")]
public sealed class ScreenContentTests(FrontendFactory web)
{
    [Fact]
    [Trait("Requirement", "REQ-002")]
    public async Task Owner_list_and_details_show_the_owner_its_assets_and_invoices()
    {
        var data = await web.CreateDataAsync();
        var browser = await web.SignInAs("Admin");

        var list = await browser.Page("/Owners");
        Assert.Contains(data.OwnerName, list);
        Assert.Contains("eigenaar@example.test", list);

        var details = await browser.Page($"/Owners/Details/{data.OwnerId}");
        Assert.Contains(data.OwnerName, details);
        Assert.Contains(data.AssetName, details);
        Assert.Contains(data.InvoiceNumber, details);
    }

    [Fact]
    [Trait("Requirement", "REQ-003")]
    public async Task Asset_list_and_details_show_the_asset_its_owner_expenses_and_activities()
    {
        var data = await web.CreateDataAsync();
        var browser = await web.SignInAs("Admin");

        var list = await browser.Page($"/Assets?OwnerId={data.OwnerId}");
        Assert.Contains(data.AssetName, list);
        Assert.Contains(data.OwnerName, list);

        var details = await browser.Page($"/Assets/Details/{data.AssetId}");
        Assert.Contains(data.AssetName, details);
        Assert.Contains("Lamp vervangen", details);
        Assert.Contains("Inspectie dak", details);
    }

    [Fact]
    [Trait("Requirement", "REQ-004")]
    [Trait("Requirement", "REQ-021")]
    public async Task Expense_list_shows_the_expense_with_VAT_in_Dutch_format()
    {
        var data = await web.CreateDataAsync();
        var page = await (await web.SignInAs("Admin")).Page($"/Expenses?AssetId={data.AssetId}");
        Assert.Contains("Lamp vervangen", page);
        Assert.Contains("121,00", page);
    }

    [Fact]
    [Trait("Requirement", "REQ-005")]
    [Trait("Requirement", "REQ-021")]
    public async Task Activity_list_shows_the_activity_with_VAT_in_Dutch_format()
    {
        var data = await web.CreateDataAsync();
        var page = await (await web.SignInAs("Admin")).Page($"/Activities?AssetId={data.AssetId}");
        Assert.Contains("Inspectie dak", page);
        Assert.Contains("242,00", page);
    }

    [Fact]
    [Trait("Requirement", "REQ-006")]
    public async Task Vat_rate_screen_lists_the_seeded_rates()
    {
        var page = await (await web.SignInAs("Admin")).Page("/VatRates");
        Assert.Contains("Hoog", page);
        Assert.Contains("Laag", page);
        Assert.Contains("Nul", page);
    }

    [Fact]
    [Trait("Requirement", "REQ-007")]
    public async Task Invoice_list_and_details_show_the_number_lines_and_totals()
    {
        var data = await web.CreateDataAsync();
        var browser = await web.SignInAs("Admin");

        Assert.Contains(data.InvoiceNumber, await browser.Page($"/Invoices?OwnerId={data.OwnerId}"));

        var details = await browser.Page($"/Invoices/Details/{data.InvoiceId}");
        Assert.Contains(data.InvoiceNumber, details);
        Assert.Contains(data.OwnerName, details);
        Assert.Contains("Lamp vervangen", details);
        Assert.Contains("Inspectie dak", details);
        Assert.Contains("€ 300,00", details);
        Assert.Contains("€ 63,00", details);
        Assert.Contains("€ 363,00", details);
    }

    [Fact]
    [Trait("Requirement", "REQ-007")]
    public async Task New_invoice_screen_shows_the_uninvoiced_lines_of_the_owner()
    {
        var data = await web.CreateDataAsync();
        var admin = await web.Api.As("Admin");
        await admin.CreateActivity(data.AssetId, "2026-04-02");

        var page = await (await web.SignInAs("Admin")).Page($"/Invoices/Create?OwnerId={data.OwnerId}");

        Assert.Contains("Inspectie dak", page);
        Assert.Contains("€ 242,00", page);
    }

    [Fact]
    [Trait("Requirement", "REQ-008")]
    [Trait("Requirement", "REQ-009")]
    public async Task Reports_show_the_asset_result_and_the_owner_overview_for_the_period()
    {
        var data = await web.CreateDataAsync();
        var page = await (await web.SignInAs("Admin")).Page("/Reports?From=2026-01-01&To=2026-12-31");
        Assert.Contains(data.AssetName, page);
        Assert.Contains(data.OwnerName, page);
    }

    [Fact]
    [Trait("Requirement", "REQ-012")]
    public async Task User_and_role_screens_list_the_users_and_roles()
    {
        var browser = await web.SignInAs("Admin");
        Assert.Contains(ApiFactory.AdminUserName, await browser.Page("/Admin/Users"));
        var roles = await browser.Page("/Admin/Roles");
        foreach (var role in FrontendFactory.Roles) Assert.Contains(role, roles);
    }

    [Fact]
    [Trait("Requirement", "REQ-007")]
    [Trait("TestCase", "TC-14")]
    public async Task Invoice_pdf_download_returns_the_pdf()
    {
        var data = await web.CreateDataAsync();
        var response = await (await web.SignInAs("Admin")).GetAsync($"/Invoices/Details/{data.InvoiceId}?handler=Pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }
}
