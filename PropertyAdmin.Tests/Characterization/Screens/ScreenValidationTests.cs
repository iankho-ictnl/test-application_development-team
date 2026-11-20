using System.Net;
using System.Text.Json;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>
/// TC-13 (REQ-030 criterion 4): per form one invalid post. The form is shown again (200) with the
/// field error (at the field where the screen has a field message, otherwise in the validation
/// summary with the expected message of the invalid field, Q-119), and nothing is saved. The screens run in the
/// default culture nl-NL.
/// </summary>
[Collection(FrontendCollection.Name)]
[Trait("TestCase", "TC-13")]
public sealed class ScreenValidationTests(FrontendFactory web)
{
    /// <summary>The Dutch text of the "Required" validation message (SharedResource.nl-NL.resx).</summary>
    private const string Required = "Verplicht";

    private static bool Has(JsonElement list, string property, string value) =>
        list.EnumerateArray().Any(e => e.GetProperty(property).GetString() == value);

    [Fact]
    [Trait("Requirement", "REQ-002")]
    public async Task Owner_form_without_a_name_shows_the_field_error_and_saves_nothing()
    {
        var browser = await web.SignInAs("Admin");
        var email = $"{web.Api.Unique("invalid")}@example.test";

        var response = await browser.Submit("/Owners/Edit", new()
        {
            ["Input.Name"] = "", ["Input.Email"] = email, ["Input.Phone"] = "0101234567", ["Input.Street"] = "Formweg", ["Input.HouseNumber"] = "3",
            ["Input.PostalCode"] = "1234 EF", ["Input.City"] = "Formstad", ["Input.Country"] = "Nederland",
        });

        await Browser.ExpectFieldError(response, "Input.Name");
        Assert.False(Has(await Http.Expect(await (await web.Api.As("Admin")).GetAsync("/api/owners"), HttpStatusCode.OK), "email", email));
    }

    [Fact]
    [Trait("Requirement", "REQ-003")]
    public async Task Asset_form_without_a_city_shows_the_field_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var ownerId = await admin.CreateOwner(web.Api.Unique("Formeigenaar"));
        var name = web.Api.Unique("Ongeldig pand");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Assets/Edit", new()
        {
            ["Input.OwnerId"] = $"{ownerId}", ["Input.Type"] = "Home", ["Input.Name"] = name, ["Input.Street"] = "Pandweg", ["Input.HouseNumber"] = "4",
            ["Input.PostalCode"] = "1234 GH", ["Input.City"] = "", ["Input.Country"] = "Nederland",
        });

        await Browser.ExpectFieldError(response, "Input.City");
        Assert.False(Has(await Http.Expect(await admin.GetAsync($"/api/assets?ownerId={ownerId}"), HttpStatusCode.OK), "name", name));
    }

    [Fact]
    [Trait("Requirement", "REQ-004")]
    public async Task Expense_form_without_a_description_shows_the_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(web.Api.Unique("Formeigenaar")), web.Api.Unique("Formpand"));
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Expenses/Edit", new()
        {
            ["Input.AssetId"] = $"{assetId}", ["Input.Date"] = "2026-04-01", ["Input.Description"] = "", ["Input.Category"] = "Schoonmaak",
            ["Input.AmountExclVat"] = "50", ["Input.VatRateName"] = "Hoog",
        });

        Assert.Contains(Required, await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
        Assert.Empty((await Http.Expect(await admin.GetAsync($"/api/expenses?assetId={assetId}"), HttpStatusCode.OK)).EnumerateArray());
    }

    [Fact]
    [Trait("Requirement", "REQ-005")]
    public async Task Activity_form_without_a_description_shows_the_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(web.Api.Unique("Formeigenaar")), web.Api.Unique("Formpand"));
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Activities/Edit", new()
        {
            ["Input.AssetId"] = $"{assetId}", ["Input.Date"] = "2026-04-01", ["Input.Kind"] = "Maintenance", ["Input.Description"] = "",
            ["Input.PricingType"] = "Hourly", ["Input.Hours"] = "2", ["Input.HourlyRate"] = "50", ["Input.VatRateName"] = "Hoog",
        });

        Assert.Contains(Required, await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
        Assert.Empty((await Http.Expect(await admin.GetAsync($"/api/activities?assetId={assetId}"), HttpStatusCode.OK)).EnumerateArray());
    }

    [Fact]
    [Trait("Requirement", "REQ-006")]
    public async Task Vat_rate_form_without_a_name_shows_the_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var before = (await Http.Expect(await admin.GetAsync("/api/vatrates"), HttpStatusCode.OK)).GetArrayLength();
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/VatRates", new() { ["Input.Name"] = "", ["Input.Percentage"] = "5", ["Input.ValidFrom"] = "2026-01-01" });

        Assert.Contains(Required, await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
        Assert.Equal(before, (await Http.Expect(await admin.GetAsync("/api/vatrates"), HttpStatusCode.OK)).GetArrayLength());
    }

    [Fact]
    [Trait("Requirement", "REQ-007")]
    public async Task Invoice_form_without_a_date_shows_the_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var ownerId = await admin.CreateOwner(web.Api.Unique("Formeigenaar"));
        await admin.CreateActivity(await admin.CreateAsset(ownerId, web.Api.Unique("Formpand")));
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit($"/Invoices/Create?OwnerId={ownerId}", new() { ["OwnerId"] = $"{ownerId}", ["Date"] = "" }, "/Invoices/Create");

        await Browser.ExpectSummaryError(response);
        var invoices = await Http.Expect(await admin.GetAsync("/api/invoices"), HttpStatusCode.OK);
        Assert.DoesNotContain(invoices.EnumerateArray(), i => i.GetProperty("ownerId").GetInt32() == ownerId);
    }

    [Fact(Skip = "BUG-14: the empty invoice date shows the framework's English text \"The value '' is invalid.\" on the Dutch screen")]
    [Trait("Requirement", "REQ-011")]
    [Trait("Bug", "BUG-14")]
    public async Task Invoice_form_without_a_date_shows_the_Dutch_required_message()
    {
        var admin = await web.Api.As("Admin");
        var ownerId = await admin.CreateOwner(web.Api.Unique("Formeigenaar"));
        await admin.CreateActivity(await admin.CreateAsset(ownerId, web.Api.Unique("Formpand")));
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit($"/Invoices/Create?OwnerId={ownerId}", new() { ["OwnerId"] = $"{ownerId}", ["Date"] = "" }, "/Invoices/Create");

        Assert.Contains(Required, await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-012")]
    public async Task Role_form_without_a_name_shows_the_error_and_saves_nothing()
    {
        var admin = await web.Api.As("Admin");
        var before = (await Http.Expect(await admin.GetAsync("/api/roles"), HttpStatusCode.OK)).GetArrayLength();
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Admin/Roles", new() { ["Name"] = "", ["SelectedPermissions"] = "Reports" });

        Assert.Contains(Required, await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
        Assert.Equal(before, (await Http.Expect(await admin.GetAsync("/api/roles"), HttpStatusCode.OK)).GetArrayLength());
    }

    [Fact]
    [Trait("Requirement", "REQ-012")]
    public async Task User_form_with_an_invalid_email_shows_the_error_and_saves_nothing()
    {
        var userName = web.Api.Unique("screen.invalid");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Admin/Users/Edit", new()
        {
            ["UserName"] = userName, ["Email"] = "no-email", ["Password"] = ApiFactory.Password, ["Roles"] = "Viewer",
        });

        Assert.Contains("Ongeldig e-mailadres", await Browser.ExpectSummaryError(response), StringComparison.Ordinal);
        Assert.False(Has(await Http.Expect(await (await web.Api.As("Admin")).GetAsync("/api/users"), HttpStatusCode.OK), "userName", userName));
    }
}
