using System.Net;
using System.Text.Json;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>TC-13: a save round trip per form: the screen posts, redirects, and the API holds the saved values.</summary>
[Collection(FrontendCollection.Name)]
[Trait("TestCase", "TC-13")]
public sealed class ScreenFormTests(FrontendFactory web)
{
    private static JsonElement Named(JsonElement list, string property, string value) =>
        list.EnumerateArray().Single(e => e.GetProperty(property).GetString() == value);

    [Fact]
    [Trait("Requirement", "REQ-002")]
    public async Task Owner_create_and_edit_save_through_the_API()
    {
        var browser = await web.SignInAs("Admin");
        var admin = await web.Api.As("Admin");
        var name = web.Api.Unique("Formeigenaar");
        var fields = new Dictionary<string, string>
        {
            ["Input.Name"] = name, ["Input.Email"] = "form@example.test", ["Input.Phone"] = "0101234567",
            ["Input.Street"] = "Formweg", ["Input.HouseNumber"] = "3", ["Input.PostalCode"] = "1234 EF", ["Input.City"] = "Formstad",
            ["Input.Country"] = "Nederland", ["Input.KvkNumber"] = "12345678", ["Input.VatNumber"] = "NL123456789B01",
        };

        Browser.ExpectRedirect(await browser.Submit("/Owners/Edit", fields), "/Owners");
        var owner = Named(await Http.Expect(await admin.GetAsync("/api/owners"), HttpStatusCode.OK), "name", name);
        Assert.Equal("form@example.test", owner.GetProperty("email").GetString());
        Assert.Equal("Formstad", owner.GetProperty("city").GetString());
        var id = owner.GetProperty("id").GetInt32();

        fields["Input.City"] = "Andere stad";
        Browser.ExpectRedirect(await browser.Submit($"/Owners/Edit/{id}", fields), "/Owners");
        Assert.Equal("Andere stad", (await Http.Expect(await admin.GetAsync($"/api/owners/{id}"), HttpStatusCode.OK)).GetProperty("city").GetString());
    }

    [Fact]
    [Trait("Requirement", "REQ-002")]
    public async Task Owner_delete_marks_the_owner_inactive()
    {
        var admin = await web.Api.As("Admin");
        var name = web.Api.Unique("Formeigenaar");
        var id = await admin.CreateOwner(name);
        var browser = await web.SignInAs("Admin");

        Browser.ExpectRedirect(await browser.Submit("/Owners", new(), $"/Owners?id={id}&handler=Delete"), "/Owners");

        // D-045 (BUG-3 fixed): the owner leaves the list but stays readable by id.
        await Http.Expect(await admin.GetAsync($"/api/owners/{id}"), HttpStatusCode.OK);
        Assert.DoesNotContain(name, await browser.Page("/Owners"));
    }

    [Fact]
    [Trait("Requirement", "REQ-002")]
    public async Task Owner_delete_refused_by_the_API_shows_the_error_and_keeps_the_owner()
    {
        var data = await web.CreateDataAsync();
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Owners", new(), $"/Owners?id={data.OwnerId}&handler=Delete");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        await Http.Expect(await (await web.Api.As("Admin")).GetAsync($"/api/owners/{data.OwnerId}"), HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Requirement", "REQ-003")]
    public async Task Asset_create_and_edit_save_through_the_API()
    {
        // BUG-7 fixed: the type travels as a number. D-041: five address fields (street, house number, postal code, city, country; foreign allowed).
        var admin = await web.Api.As("Admin");
        var ownerId = await admin.CreateOwner(web.Api.Unique("Formeigenaar"));
        var name = web.Api.Unique("Formpand");
        var browser = await web.SignInAs("Admin");
        var fields = new Dictionary<string, string>
        {
            ["Input.OwnerId"] = $"{ownerId}", ["Input.Type"] = "Home", ["Input.Name"] = name,
            ["Input.Street"] = "Pandweg", ["Input.HouseNumber"] = "4a", ["Input.PostalCode"] = "1234 GH", ["Input.City"] = "Pandstad",
            ["Input.Country"] = "Nederland",
        };

        Browser.ExpectRedirect(await browser.Submit("/Assets/Edit", fields), "/Assets");
        var asset = Named(await Http.Expect(await admin.GetAsync($"/api/assets?ownerId={ownerId}"), HttpStatusCode.OK), "name", name);
        Assert.Equal("Pandweg", asset.GetProperty("street").GetString());
        Assert.Equal("4a", asset.GetProperty("houseNumber").GetString());
        Assert.Equal("1234 GH", asset.GetProperty("postalCode").GetString());
        Assert.Equal("Pandstad", asset.GetProperty("city").GetString());
        Assert.Equal("Nederland", asset.GetProperty("country").GetString());
        var id = asset.GetProperty("id").GetInt32();

        fields["Input.City"] = "Antwerpen";
        fields["Input.PostalCode"] = "2000";
        fields["Input.Country"] = "België";
        Browser.ExpectRedirect(await browser.Submit($"/Assets/Edit/{id}", fields), "/Assets");
        var saved = await Http.Expect(await admin.GetAsync($"/api/assets/{id}"), HttpStatusCode.OK);
        Assert.Equal("Antwerpen", saved.GetProperty("city").GetString());
        Assert.Equal("2000", saved.GetProperty("postalCode").GetString());
        Assert.Equal("België", saved.GetProperty("country").GetString());
    }

    [Fact]
    [Trait("Requirement", "REQ-004")]
    public async Task Expense_create_saves_through_the_API_with_VAT()
    {
        var admin = await web.Api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(web.Api.Unique("Formeigenaar")), web.Api.Unique("Formpand"));
        var description = web.Api.Unique("Ramen wassen");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Expenses/Edit", new()
        {
            ["Input.AssetId"] = $"{assetId}", ["Input.Date"] = "2026-04-01", ["Input.Description"] = description, ["Input.Category"] = "Schoonmaak",
            ["Input.AmountExclVat"] = "50", ["Input.VatRateName"] = "Hoog", ["Input.Reinvoiceable"] = "true",
        });

        Browser.ExpectRedirect(response, $"/Assets/Details/{assetId}");
        var expense = Named(await Http.Expect(await admin.GetAsync($"/api/expenses?assetId={assetId}"), HttpStatusCode.OK), "description", description);
        Assert.Equal(60.50m, expense.GetProperty("amountInclVat").GetDecimal());
        Assert.True(expense.GetProperty("reinvoiceable").GetBoolean());
    }

    [Fact]
    [Trait("Requirement", "REQ-005")]
    public async Task Activity_create_saves_through_the_API_with_hours_times_rate_and_VAT()
    {
        // BUG-7 fixed: kind and pricing type travel as numbers.
        var admin = await web.Api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(web.Api.Unique("Formeigenaar")), web.Api.Unique("Formpand"));
        var description = web.Api.Unique("Cv-ketel onderhoud");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Activities/Edit", new()
        {
            ["Input.AssetId"] = $"{assetId}", ["Input.Date"] = "2026-04-01", ["Input.Kind"] = "Maintenance", ["Input.Description"] = description,
            ["Input.PricingType"] = "Hourly", ["Input.Hours"] = "2", ["Input.HourlyRate"] = "50", ["Input.VatRateName"] = "Hoog",
        });

        Browser.ExpectRedirect(response, $"/Assets/Details/{assetId}");
        var activity = Named(await Http.Expect(await admin.GetAsync($"/api/activities?assetId={assetId}"), HttpStatusCode.OK), "description", description);
        Assert.Equal("2026-04-01", activity.GetProperty("date").GetString());
        Assert.Equal(100.00m, activity.GetProperty("amountExclVat").GetDecimal());
        Assert.Equal(121.00m, activity.GetProperty("amountInclVat").GetDecimal());
    }

    [Fact]
    [Trait("Requirement", "REQ-006")]
    public async Task Vat_rate_create_saves_through_the_API()
    {
        var name = web.Api.Unique("Tarief");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/VatRates", new() { ["Input.Name"] = name, ["Input.Percentage"] = "5", ["Input.ValidFrom"] = "2026-01-01" });

        Browser.ExpectRedirect(response, "/VatRates");
        var rate = Named(await Http.Expect(await (await web.Api.As("Admin")).GetAsync("/api/vatrates"), HttpStatusCode.OK), "name", name);
        Assert.Equal(5m, rate.GetProperty("percentage").GetDecimal());
        Assert.Equal("2026-01-01", rate.GetProperty("validFrom").GetString());
    }

    [Fact]
    [Trait("Requirement", "REQ-007")]
    public async Task Invoice_create_and_mark_paid_save_through_the_API()
    {
        var admin = await web.Api.As("Admin");
        var ownerId = await admin.CreateOwner(web.Api.Unique("Formeigenaar"));
        await admin.CreateActivity(await admin.CreateAsset(ownerId, web.Api.Unique("Formpand")));
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit($"/Invoices/Create?OwnerId={ownerId}", new() { ["OwnerId"] = $"{ownerId}", ["Date"] = "2026-04-30", ["PaymentTermDays"] = "14" }, "/Invoices/Create");

        var location = response.Headers.Location?.OriginalString ?? "";
        var id = int.Parse(location[(location.LastIndexOf('/') + 1)..]);
        Browser.ExpectRedirect(response, $"/Invoices/Details/{id}");
        var invoice = await Http.Expect(await admin.GetAsync($"/api/invoices/{id}"), HttpStatusCode.OK);
        Assert.Equal(ownerId, invoice.GetProperty("ownerId").GetInt32());
        Assert.Equal(200.00m, invoice.GetProperty("totalExclVat").GetDecimal());
        Assert.Equal("2026-05-14", invoice.GetProperty("dueDate").GetString());

        response = await browser.Submit($"/Invoices/Details/{id}", new() { ["PaidDate"] = "2026-05-10" }, $"/Invoices/Details/{id}?handler=Pay");

        Browser.ExpectRedirect(response, $"/Invoices/Details/{id}");
        Assert.Equal("2026-05-10", (await Http.Expect(await admin.GetAsync($"/api/invoices/{id}"), HttpStatusCode.OK)).GetProperty("paidDate").GetString());
    }

    [Fact]
    [Trait("Requirement", "REQ-012")]
    public async Task Role_create_saves_through_the_API()
    {
        var name = web.Api.Unique("Schermrol");
        var browser = await web.SignInAs("Admin");

        Browser.ExpectRedirect(await browser.Submit("/Admin/Roles", new() { ["Name"] = name, ["SelectedPermissions"] = "Reports" }), "/Admin/Roles");

        var role = Named(await Http.Expect(await (await web.Api.As("Admin")).GetAsync("/api/roles"), HttpStatusCode.OK), "name", name);
        Assert.Equal(["Reports"], role.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    [Trait("Requirement", "REQ-012")]
    public async Task User_create_saves_through_the_API_and_the_new_user_signs_in_with_the_role()
    {
        var userName = web.Api.Unique("screen.new");
        var browser = await web.SignInAs("Admin");

        var response = await browser.Submit("/Admin/Users/Edit", new()
        {
            ["UserName"] = userName, ["Email"] = $"{userName}@example.test", ["Password"] = ApiFactory.Password, ["Roles"] = "Viewer",
        });

        Browser.ExpectRedirect(response, "/Admin/Users");
        var user = await web.SignIn(userName);
        await user.Page("/Reports");
        Browser.ExpectRedirect(await user.GetAsync("/VatRates"), "/Account/AccessDenied");
    }
}
