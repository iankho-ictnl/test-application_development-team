using System.Net;
using System.Text.RegularExpressions;

namespace PropertyAdmin.Tests.Demo;

/// <summary>
/// Rule 15, D-047, T-112: the S2 demo of plan.md, step by step in its order, through the real screens, the real API
/// and a real PostgreSQL 18, signed in through /Account/Login as the seed admin, in Dutch and in English.
/// S2 demo: "/Owners: save with only a name gives field errors; a foreign address and DE123456789 are accepted,
/// XX123 is refused; delete an owner without assets: gone from the list, still on its invoices; owner with assets:
/// refused". Traces: REQ-002 (US-2.1, US-2.1a, US-2.1b, US-2.1d, US-2.3, D-041, D-045), REQ-011, REQ-030.
/// The API message texts (D-049) are checked once F8 is merged; until then the steps check where the error is shown.
/// </summary>
[Collection(DemoCollection.Name)]
[Trait("Category", "Demo")]
public sealed partial class S2DemoTests(DemoStack stack)
{
    [Theory]
    [InlineData("nl-NL", "nl")]
    [InlineData("en-US", "en")]
    public async Task S2_demo_owners_complete_and_never_deleted(string culture, string lang)
    {
        using var browser = stack.NewBrowser();
        var tag = lang.ToUpperInvariant();

        // Sign in like the user (TC-DEMO-S2-1, REQ-001, C6).
        await browser.PostFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["UserName"] = stack.AdminUserName,
            ["Password"] = stack.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.DoesNotContain("/Account/Login", browser.Path);

        // Switch the language (TC-DEMO-S2-2, REQ-011).
        await browser.PostFormAsync("/Owners", new Dictionary<string, string> { ["culture"] = culture, ["returnUrl"] = "/Owners" }, "/Account/Culture");
        Assert.Equal("/Owners", browser.Path);
        Assert.Contains($"<html lang=\"{lang}\">", browser.Html);

        // Step 1 (TC-DEMO-S2-3, US-2.1, D-041): save with only a name gives field errors.
        await browser.PostFormAsync("/Owners/Edit", new Dictionary<string, string> { ["Input.Name"] = $"Alleen naam {tag}" });
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.StartsWith("/Owners/Edit", browser.Path);
        foreach (var field in new[] { "Email", "Phone", "Street", "HouseNumber", "PostalCode", "City", "Country" })
            Assert.True(HasFieldError(browser.Html, field), $"no field error at {field} ({culture})");

        // Step 2 (TC-DEMO-S2-4, US-2.1a, US-2.1d): a foreign address and DE123456789 are accepted.
        var foreign = $"Schmidt GmbH {tag}";
        await browser.PostFormAsync("/Owners/Edit", Owner(foreign, vat: "DE123456789"));
        Assert.Equal("/Owners", browser.Path);
        Assert.Contains(WebUtility.HtmlEncode(foreign), browser.Html);

        // Step 3 (TC-DEMO-S2-5, US-2.1d): XX123 is refused at the VAT field.
        await browser.PostFormAsync("/Owners/Edit", Owner($"Ongeldig btw {tag}", vat: "XX123"));
        Assert.StartsWith("/Owners/Edit", browser.Path);
        Assert.True(HasFieldError(browser.Html, "VatNumber"), $"XX123 not refused at the VAT field ({culture})");
        Assert.DoesNotContain(WebUtility.HtmlEncode($"Ongeldig btw {tag}"), await browser.GetAsync("/Owners"));

        // Prepare step 4: an owner with an invoice and no assets left (its asset moves to the foreign owner).
        var leaving = $"Vertrekt BV {tag}";
        await browser.PostFormAsync("/Owners/Edit", Owner(leaving));
        var leavingId = OwnerId(browser.Html, leaving);
        var foreignId = OwnerId(browser.Html, foreign);
        var asset = $"Pand {tag}";
        await browser.PostFormAsync("/Assets/Edit", new Dictionary<string, string>
        {
            ["Input.OwnerId"] = leavingId.ToString(), ["Input.Type"] = "Building", ["Input.Name"] = asset,
            ["Input.Address"] = "Dorpsstraat 1", ["Input.PostalCode"] = "1234 AB", ["Input.City"] = "Ergens",
        });
        Assert.Equal("/Assets", browser.Path);
        var assetId = LinkId(browser.Html, "Assets", asset);
        await browser.PostFormAsync("/Activities/Edit", new Dictionary<string, string>
        {
            ["Input.AssetId"] = assetId.ToString(), ["Input.Kind"] = "Administration", ["Input.Description"] = "Jaarafrekening",
            ["Input.PricingType"] = "Fixed", ["Input.FixedPrice"] = "100", ["Input.VatRateName"] = "Hoog",
        });
        Assert.Equal($"/Assets/Details/{assetId}", browser.Path);
        await browser.PostFormAsync("/Invoices/Create", new Dictionary<string, string>
        {
            ["OwnerId"] = leavingId.ToString(), ["PaymentTermDays"] = "14",
        });
        var invoice = InvoicePath().Match(browser.Path);
        Assert.True(invoice.Success, $"invoice not created ({culture}): {browser.Path}");

        // Step 5 (TC-DEMO-S2-7, US-2.3): an owner with assets is refused.
        await browser.PostFormAsync("/Owners", new Dictionary<string, string>(), $"/Owners?id={leavingId}&handler=Delete");
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.Contains("validation-summary-errors", browser.Html);
        Assert.Contains(WebUtility.HtmlEncode(leaving), await browser.GetAsync("/Owners"));

        // Move the asset away (invoiced lines only), so the owner has no assets left.
        await browser.PostFormAsync($"/Assets/Edit/{assetId}", new Dictionary<string, string>
        {
            ["Input.OwnerId"] = foreignId.ToString(), ["Input.Type"] = "Building", ["Input.Name"] = asset,
            ["Input.Address"] = "Dorpsstraat 1", ["Input.PostalCode"] = "1234 AB", ["Input.City"] = "Ergens",
        });
        Assert.Equal("/Assets", browser.Path);

        // Step 4 (TC-DEMO-S2-6, US-2.3, D-045): delete an owner without assets: gone from the list, still on its invoice.
        await browser.PostFormAsync("/Owners", new Dictionary<string, string>(), $"/Owners?id={leavingId}&handler=Delete");
        Assert.Equal("/Owners", browser.Path);
        Assert.DoesNotContain(WebUtility.HtmlEncode(leaving), browser.Html);
        Assert.Contains(WebUtility.HtmlEncode(leaving), await browser.GetAsync(invoice.Value));
        var pdf = await browser.GetBytesAsync($"{invoice.Value}?handler=Pdf");
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    private static Dictionary<string, string> Owner(string name, string? vat = null) => new()
    {
        ["Input.Name"] = name, ["Input.Email"] = "eigenaar@example.test", ["Input.Phone"] = "+49 30 1234567",
        ["Input.Street"] = "Unter den Linden", ["Input.HouseNumber"] = "77", ["Input.PostalCode"] = "10117",
        ["Input.City"] = "Berlin", ["Input.Country"] = "Duitsland", ["Input.KvkNumber"] = "", ["Input.VatNumber"] = vat ?? "",
    };

    private static bool HasFieldError(string html, string field) =>
        Regex.IsMatch(html, $"class=\"[^\"]*field-validation-error[^\"]*\" data-valmsg-for=\"Input.{field}\"");

    private static int OwnerId(string html, string name) => LinkId(html, "Owners", name);

    private static int LinkId(string html, string feature, string name)
    {
        var m = Regex.Match(html, $"href=\"/{feature}/Details/(\\d+)\">{Regex.Escape(WebUtility.HtmlEncode(name))}<");
        Assert.True(m.Success, $"no link to {name} in /{feature}");
        return int.Parse(m.Groups[1].Value);
    }

    [GeneratedRegex("^/Invoices/Details/\\d+$")]
    private static partial Regex InvoicePath();
}
