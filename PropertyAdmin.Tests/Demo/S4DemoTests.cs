using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace PropertyAdmin.Tests.Demo;

/// <summary>
/// Rule 15, D-047, T-166: the S4 demo of plan.md, step by step in its order, through the real screens, the real API
/// and a real PostgreSQL 18, signed in through /Account/Login as the seed admin, in Dutch and in English.
/// S4 demo: "/Invoices/Create twice: consecutive numbers, due date + 14 days; only the most recent unpaid invoice can be
/// deleted (Q-083)". Traces: REQ-007 (D-056, D-057, BUG-6), REQ-024 criterion 1 (D-056), REQ-011.
/// Still to come in this test (T-166): the /Reports step ("to" before "from": field error) once REQ-008 is in this branch,
/// and the other S4 demo steps with their requirements.
/// </summary>
[Collection(DemoCollection.Name)]
[Trait("Category", "Demo")]
public sealed class S4DemoTests(DemoStack stack)
{
    [Theory]
    [InlineData("nl-NL", "nl", "Alleen de meest recente onbetaalde factuur kan worden verwijderd.")]
    [InlineData("en-US", "en", "Only the most recent unpaid invoice can be deleted.")]
    public async Task S4_demo_invoices_are_numbered_without_gaps_with_a_14_day_term_and_only_the_most_recent_can_be_deleted(
        string culture, string lang, string refusal)
    {
        using var browser = stack.NewBrowser();
        var tag = lang.ToUpperInvariant();

        // Sign in like the user (TC-DEMO-S4-1, REQ-001).
        await browser.PostFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["UserName"] = stack.AdminUserName,
            ["Password"] = stack.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.DoesNotContain("/Account/Login", browser.Path);

        // Switch the language (TC-DEMO-S4-2, REQ-011).
        await browser.PostFormAsync("/Invoices", new Dictionary<string, string> { ["culture"] = culture, ["returnUrl"] = "/Invoices" }, "/Account/Culture");
        Assert.Equal("/Invoices", browser.Path);
        Assert.Contains($"<html lang=\"{lang}\">", browser.Html);

        // An owner with an asset to invoice.
        var owner = $"Eigenaar S4 {tag}";
        await browser.PostFormAsync("/Owners/Edit", Owner(owner));
        Assert.Equal("/Owners", browser.Path);
        var ownerId = LinkId(browser.Html, "Owners", owner);
        await browser.PostFormAsync("/Assets/Edit", Asset(ownerId, $"Pand S4 {tag}"));
        Assert.Equal("/Assets", browser.Path);
        var assetId = LinkId(browser.Html, "Assets", $"Pand S4 {tag}");

        // Step 1 (TC-DEMO-S4-3, REQ-007, D-056, D-057): /Invoices/Create twice gives consecutive numbers, each due 14 days
        // after its date.
        var first = await CreateInvoiceAsync(browser, assetId, ownerId, $"Eerste klus S4 {tag}", culture);
        var second = await CreateInvoiceAsync(browser, assetId, ownerId, $"Tweede klus S4 {tag}", culture);
        Assert.Equal(first.Year, second.Year);
        Assert.Equal(first.Sequence + 1, second.Sequence);

        // Step 2 (TC-DEMO-S4-4, REQ-007, REQ-024 criterion 1, D-056, Q-083 c): the older open invoice cannot be deleted, and
        // the screen gives the reason in the chosen language.
        await browser.PostFormAsync($"/Invoices/Details/{first.Id}", new Dictionary<string, string>(),
            $"/Invoices/Details/{first.Id}?handler=Delete");
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.StartsWith($"/Invoices/Details/{first.Id}", browser.Path);
        Assert.Contains(WebUtility.HtmlEncode(refusal), browser.Html);
        Assert.Contains(first.Number, await browser.GetAsync("/Invoices"));

        // Step 3 (TC-DEMO-S4-5, REQ-007, D-056): the most recent open invoice can be deleted; the older one stays.
        await browser.PostFormAsync($"/Invoices/Details/{second.Id}", new Dictionary<string, string>(),
            $"/Invoices/Details/{second.Id}?handler=Delete");
        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.Equal("/Invoices", browser.Path);
        Assert.DoesNotContain(second.Number, browser.Html);
        Assert.Contains(first.Number, browser.Html);
        Assert.Equal(HttpStatusCode.NotFound, await StatusOfAsync(browser, $"/Invoices/Details/{second.Id}"));
    }

    private sealed record CreatedInvoice(int Id, string Number, int Year, int Sequence);

    /// <summary>Records an uninvoiced activity, invoices it through /Invoices/Create and checks the details screen.</summary>
    private static async Task<CreatedInvoice> CreateInvoiceAsync(Browser browser, int assetId, int ownerId, string activity, string culture)
    {
        await browser.PostFormAsync("/Activities/Edit", new Dictionary<string, string>
        {
            ["Input.AssetId"] = assetId.ToString(CultureInfo.InvariantCulture),
            ["Input.Kind"] = "Maintenance",
            ["Input.Description"] = activity,
            ["Input.PricingType"] = "Fixed",
            ["Input.FixedPrice"] = "100",
            ["Input.VatRateName"] = "Hoog",
        });
        Assert.Equal($"/Assets/Details/{assetId}", browser.Path);

        await browser.PostFormAsync($"/Invoices/Create?OwnerId={ownerId}", new Dictionary<string, string>
        {
            ["OwnerId"] = ownerId.ToString(CultureInfo.InvariantCulture),
        }, "/Invoices/Create");
        var path = Regex.Match(browser.Path, "^/Invoices/Details/(\\d+)$");
        Assert.True(path.Success, $"no invoice created for {activity} ({culture}): {browser.Path}");
        Assert.Contains(WebUtility.HtmlEncode(activity), browser.Html);

        var number = Regex.Match(browser.Html, "<h1 class=\"h3\">[^<]*?(\\d{4})-(\\d{4})</h1>");
        Assert.True(number.Success, $"no invoice number on the details screen ({culture})");
        var format = CultureInfo.GetCultureInfo(culture);
        var date = DateOnly.Parse(Field(browser.Html, 1), format);
        var due = DateOnly.Parse(Field(browser.Html, 2), format);
        Assert.Equal(date.AddDays(14), due);
        return new CreatedInvoice(int.Parse(path.Groups[1].Value, CultureInfo.InvariantCulture),
            $"{number.Groups[1].Value}-{number.Groups[2].Value}",
            int.Parse(number.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(number.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>The value of the n-th plain-text field of the details list (0 owner, 1 date, 2 due date, 3 status).</summary>
    private static string Field(string html, int index)
    {
        var values = Regex.Matches(html, "<dd class=\"col-sm-9\">([^<]*)</dd>");
        var plain = values.Select(m => m.Groups[1].Value.Trim()).ToList();
        // The owner field holds a link, so the plain fields start at the date.
        Assert.True(plain.Count >= index, "the invoice details list is not complete");
        return WebUtility.HtmlDecode(plain[index - 1]);
    }

    private static async Task<HttpStatusCode> StatusOfAsync(Browser browser, string url)
    {
        await browser.GetAsync(url);
        return browser.Status;
    }

    private static Dictionary<string, string> Owner(string name) => new()
    {
        ["Input.Name"] = name,
        ["Input.Email"] = "eigenaar@example.test",
        ["Input.Phone"] = "+31 20 1234567",
        ["Input.Street"] = "Kerkstraat",
        ["Input.HouseNumber"] = "4",
        ["Input.PostalCode"] = "1234 AB",
        ["Input.City"] = "Ergens",
        ["Input.Country"] = "Nederland",
        ["Input.KvkNumber"] = "",
        ["Input.VatNumber"] = "",
    };

    private static Dictionary<string, string> Asset(int ownerId, string name) => new()
    {
        ["Input.OwnerId"] = ownerId.ToString(CultureInfo.InvariantCulture),
        ["Input.Type"] = "Building",
        ["Input.Name"] = name,
        ["Input.Street"] = "Dorpsstraat",
        ["Input.HouseNumber"] = "8",
        ["Input.PostalCode"] = "5678 CD",
        ["Input.City"] = "Ergens",
        ["Input.Country"] = "Nederland",
    };

    private static int LinkId(string html, string feature, string name)
    {
        var m = Regex.Match(html, $"href=\"/{feature}/Details/(\\d+)\">{Regex.Escape(WebUtility.HtmlEncode(name))}<");
        Assert.True(m.Success, $"no link to {name} in /{feature}");
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }
}
