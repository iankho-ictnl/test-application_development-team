using System.Net;
using System.Text.RegularExpressions;
using PropertyAdmin.Tests.Demo;

namespace PropertyAdmin.Tests.Frontend.Reports;

/// <summary>
/// T-164 (T17), REQ-008, D-033: /Reports through the real frontend, the real API and a real PostgreSQL 18 (D-047 stack),
/// signed in through /Account/Login. A reversed period shows a field error at To in the chosen language and no report;
/// a valid period shows the report without errors.
/// </summary>
[Collection(DemoCollection.Name)]
[Trait("Category", "Demo")]
public sealed partial class ReportPeriodTests(DemoStack stack)
{
    /// <summary>TC-REQ008-PERIOD-3: 'to' before 'from' gives the translated field error at To, nothing is swapped.</summary>
    [Theory]
    [InlineData("nl-NL", "'Tot en met' mag niet vóór 'Van' liggen")]
    [InlineData("en-US", "'To' must not be before 'From'")]
    public async Task Reversed_period_shows_the_field_error_at_To(string culture, string message)
    {
        using var browser = await SignedIn(culture);

        await browser.GetAsync("/Reports?From=2025-12-31&To=2025-01-01");

        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.Equal(message, FieldError(browser.Html, "To"));
        Assert.Null(FieldError(browser.Html, "From"));
        Assert.Contains("value=\"2025-12-31\"", browser.Html); // From kept as entered, not swapped
    }

    /// <summary>TC-REQ008-PERIOD-4: a valid period shows the report and no field error.</summary>
    [Theory]
    [InlineData("nl-NL")]
    [InlineData("en-US")]
    public async Task Valid_period_shows_no_field_error(string culture)
    {
        using var browser = await SignedIn(culture);

        await browser.GetAsync("/Reports?From=2025-01-01&To=2025-12-31");

        Assert.Equal(HttpStatusCode.OK, browser.Status);
        Assert.Null(FieldError(browser.Html, "From"));
        Assert.Null(FieldError(browser.Html, "To"));
        Assert.DoesNotContain("validation-summary-errors", browser.Html);
    }

    private async Task<Browser> SignedIn(string culture)
    {
        var browser = stack.NewBrowser();
        await browser.PostFormAsync("/Account/Login", new Dictionary<string, string>
        {
            ["UserName"] = stack.AdminUserName,
            ["Password"] = stack.AdminPassword,
        });
        Assert.DoesNotContain("/Account/Login", browser.Path);
        await browser.PostFormAsync("/Reports", new Dictionary<string, string> { ["culture"] = culture, ["returnUrl"] = "/Reports" }, "/Account/Culture");
        Assert.Equal("/Reports", browser.Path);
        return browser;
    }

    private static string? FieldError(string html, string field)
    {
        foreach (Match span in SpanRegex().Matches(html))
        {
            var tag = span.Groups[1].Value;
            if (tag.Contains($"data-valmsg-for=\"{field}\"") && tag.Contains("field-validation-error"))
                return WebUtility.HtmlDecode(span.Groups[2].Value).Trim();
        }
        return null;
    }

    [GeneratedRegex("<span([^>]*)>(.*?)</span>", RegexOptions.Singleline)]
    private static partial Regex SpanRegex();
}
