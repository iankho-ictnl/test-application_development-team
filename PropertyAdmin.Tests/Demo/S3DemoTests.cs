using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace PropertyAdmin.Tests.Demo;

/// <summary>
/// Rule 15, D-047, T-138: the S3 demo of plan.md, step by step in its order, through the real screens, the real API
/// and a real PostgreSQL 18, signed in through /Account/Login as the seed admin, in Dutch and in English.
/// S3 demo: "/Assets/Edit: country required and shown in /Assets; change the owner of an asset with an uninvoiced
/// activity; /Invoices/Create for the new owner lists it". Traces: REQ-003 (US-3.1, D-041, BUG-4), REQ-007, REQ-011, REQ-030.
/// </summary>
[Collection(DemoCollection.Name)]
[Trait("Category", "Demo")]
public sealed class S3DemoTests(DemoStack stack)
{
    [Theory]
    [InlineData("nl-NL", "nl")]
    [InlineData("en-US", "en")]
    public async Task S3_demo_assets_have_a_country_and_move_with_their_uninvoiced_lines(string culture, string lang)
    {
        using var browser = stack.NewBrowser();
        var tag = lang.ToUpperInvariant();

        // Sign in like the user (TC-DEMO-S3-1, REQ-001).
        await browser.PostFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["UserName"] = stack.AdminUserName,
            ["Password"] = stack.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.DoesNotContain("/Account/Login", browser.Path);

        // Switch the language (TC-DEMO-S3-2, REQ-011).
        await browser.PostFormAsync("/Assets", new Dictionary<string, string> { ["culture"] = culture, ["returnUrl"] = "/Assets" }, "/Account/Culture");
        Assert.Equal("/Assets", browser.Path);
        Assert.Contains($"<html lang=\"{lang}\">", browser.Html);

        // An owner to hold the asset.
        var owner = $"Eerste eigenaar S3 {tag}";
        await browser.PostFormAsync("/Owners/Edit", Owner(owner));
        Assert.Equal("/Owners", browser.Path);
        var ownerId = LinkId(browser.Html, "Owners", owner);

        // Step 1 (TC-DEMO-S3-3, US-3.1, D-041): the country is required, like the other address fields.
        var asset = $"Pand S3 {tag}";
        var withoutCountry = Asset(ownerId, asset, country: "");
        await browser.PostFormAsync("/Assets/Edit", withoutCountry);
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.StartsWith("/Assets/Edit", browser.Path);
        Assert.True(HasFieldError(browser.Html, "Country"), $"no field error at Country ({culture})");
        Assert.DoesNotContain(WebUtility.HtmlEncode(asset), await browser.GetAsync("/Assets"));

        // TC-DEMO-S3-4 (US-3.1, D-041): saving with only a name gives a field error at every address field.
        await browser.PostFormAsync("/Assets/Edit", new Dictionary<string, string>
        {
            ["Input.OwnerId"] = ownerId.ToString(CultureInfo.InvariantCulture), ["Input.Type"] = "Building", ["Input.Name"] = asset,
        });
        Assert.StartsWith("/Assets/Edit", browser.Path);
        foreach (var field in new[] { "Street", "HouseNumber", "PostalCode", "City", "Country" })
            Assert.True(HasFieldError(browser.Html, field), $"no field error at {field} ({culture})");

        // Step 2 (TC-DEMO-S3-5, US-3.1, D-041): with a country it is saved, and /Assets shows the country.
        await browser.PostFormAsync("/Assets/Edit", Asset(ownerId, asset, country: "Duitsland"));
        Assert.Equal("/Assets", browser.Path);
        var assetId = LinkId(browser.Html, "Assets", asset);
        var row = Regex.Match(browser.Html, $"href=\"/Assets/Details/{assetId}\">.*?</tr>", RegexOptions.Singleline);
        Assert.True(row.Success, $"no row for {asset} in /Assets ({culture})");
        Assert.Contains("Hauptstrasse 5, 10115 Berlin, Duitsland", row.Value);
        Assert.Contains("Hauptstrasse 5, 10115 Berlin, Duitsland", await browser.GetAsync($"/Assets/Details/{assetId}"));

        // Step 3 (TC-DEMO-S3-6, US-3.1, BUG-4, Q-027): an asset with an uninvoiced activity moves to another owner.
        var activity = $"Onderhoud dak S3 {tag}";
        await browser.PostFormAsync("/Activities/Edit", new Dictionary<string, string>
        {
            ["Input.AssetId"] = assetId.ToString(CultureInfo.InvariantCulture), ["Input.Kind"] = "Maintenance", ["Input.Description"] = activity,
            ["Input.PricingType"] = "Fixed", ["Input.FixedPrice"] = "250", ["Input.VatRateName"] = "Hoog",
        });
        Assert.Equal($"/Assets/Details/{assetId}", browser.Path);
        var newOwner = $"Nieuwe eigenaar S3 {tag}";
        await browser.PostFormAsync("/Owners/Edit", Owner(newOwner));
        var newOwnerId = LinkId(browser.Html, "Owners", newOwner);
        await browser.PostFormAsync($"/Assets/Edit/{assetId}", Asset(newOwnerId, asset, country: "Duitsland"));
        Assert.Equal("/Assets", browser.Path);
        Assert.DoesNotContain("validation-summary-errors", browser.Html);
        Assert.Contains(WebUtility.HtmlEncode(newOwner), await browser.GetAsync($"/Assets/Details/{assetId}"));

        // Step 4 (TC-DEMO-S3-7, BUG-4, REQ-007): /Invoices/Create lists the activity for the new owner, not the old one,
        // and the new owner's invoice holds it.
        Assert.Contains(WebUtility.HtmlEncode(activity), await browser.GetAsync($"/Invoices/Create?OwnerId={newOwnerId}"));
        Assert.DoesNotContain(WebUtility.HtmlEncode(activity), await browser.GetAsync($"/Invoices/Create?OwnerId={ownerId}"));
        await browser.PostFormAsync("/Invoices/Create", new Dictionary<string, string>
        {
            ["OwnerId"] = newOwnerId.ToString(CultureInfo.InvariantCulture), ["PaymentTermDays"] = "14",
        });
        Assert.Matches("^/Invoices/Details/\\d+$", browser.Path);
        Assert.Contains(WebUtility.HtmlEncode(newOwner), browser.Html);
        Assert.Contains(WebUtility.HtmlEncode(activity), browser.Html);
    }

    private static Dictionary<string, string> Owner(string name) => new()
    {
        ["Input.Name"] = name, ["Input.Email"] = "eigenaar@example.test", ["Input.Phone"] = "+31 20 1234567",
        ["Input.Street"] = "Kerkstraat", ["Input.HouseNumber"] = "3", ["Input.PostalCode"] = "1234 AB",
        ["Input.City"] = "Ergens", ["Input.Country"] = "Nederland", ["Input.KvkNumber"] = "", ["Input.VatNumber"] = "",
    };

    // D-041: the asset address as five required fields.
    private static Dictionary<string, string> Asset(int ownerId, string name, string country) => new()
    {
        ["Input.OwnerId"] = ownerId.ToString(CultureInfo.InvariantCulture), ["Input.Type"] = "Building", ["Input.Name"] = name,
        ["Input.Street"] = "Hauptstrasse", ["Input.HouseNumber"] = "5", ["Input.PostalCode"] = "10115",
        ["Input.City"] = "Berlin", ["Input.Country"] = country,
    };

    private static bool HasFieldError(string html, string field) =>
        Regex.IsMatch(html, $"<span[^>]*field-validation-error[^>]*data-valmsg-for=\"Input.{field}\"")
        || Regex.IsMatch(html, $"<span[^>]*data-valmsg-for=\"Input.{field}\"[^>]*field-validation-error");

    private static int LinkId(string html, string feature, string name)
    {
        var m = Regex.Match(html, $"href=\"/{feature}/Details/(\\d+)\">{Regex.Escape(WebUtility.HtmlEncode(name))}<");
        Assert.True(m.Success, $"no link to {name} in /{feature}");
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
