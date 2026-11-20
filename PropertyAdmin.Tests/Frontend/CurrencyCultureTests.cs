extern alias Frontend;

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// BUG-9 (REQ-018, REQ-011, REQ-021): money is shown in euro in every language. The culture keeps its own
/// separators (nl-NL 1.234,50; en-US 1,234.50); only the currency symbol is the euro sign.
/// </summary>
public sealed class CurrencyCultureTests : IClassFixture<DateFieldTests.FrontendFactory>
{
    private readonly DateFieldTests.FrontendFactory _factory;

    public CurrencyCultureTests(DateFieldTests.FrontendFactory factory) => _factory = factory;

    [Theory]
    [InlineData("nl-NL", "€ 1.234,50")]
    [InlineData("en-US", "€1,234.50")]
    public void Culture_formats_money_in_euro_with_its_own_separators(string name, string expected)
    {
        var culture = AppCultures.Create(name);

        Assert.Equal(expected, 1234.5m.ToString("C", culture));
        Assert.Equal(name, culture.Name);
        Assert.True(culture.IsReadOnly);
    }

    [Fact]
    public void WithEuro_also_works_on_a_read_only_culture_and_leaves_it_unchanged()
    {
        var readOnly = CultureInfo.ReadOnly(new CultureInfo("en-US"));

        var euro = AppCultures.WithEuro(readOnly);

        Assert.Equal("€", euro.NumberFormat.CurrencySymbol);
        Assert.True(euro.IsReadOnly);
        Assert.Equal("$", readOnly.NumberFormat.CurrencySymbol);
    }

    [Theory]
    [InlineData("nl-NL", "121,00")]
    [InlineData("en-US", "121.00")]
    public async Task Invoice_total_is_rendered_in_euro(string culture, string amount)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c%3D{culture}%7Cuic%3D{culture}");

        using var response = await client.GetAsync("/Invoices/Create?ownerId=1");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(new Regex($"€\\s?{Regex.Escape(amount)}"), html);
        Assert.DoesNotContain("$", Regex.Match(html, "<p class=\"fw-bold\">[^<]*</p>").Value);
    }
}
