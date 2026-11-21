extern alias Frontend;

using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PropertyAdmin.Contracts;
using FrontendProgram = Frontend::Program;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend.Reports;

/// <summary>
/// REQ-009 (US-9.1, US-9.4), REQ-011, T-172 (F12): /Reports lists under each owner its open invoices with number
/// (link to the invoice), date, due date, total incl. VAT and an overdue mark, in Dutch and English. The overdue
/// flag comes from the API. The API is a test double with the B23 contract (B23 is not built yet).
/// </summary>
public sealed class OwnerOverviewOpenInvoicesTests : IClassFixture<OwnerOverviewOpenInvoicesTests.FrontendFactory>
{
    private const string Overview = """
        [{"ownerId":1,"ownerName":"Jansen","assetCount":1,"income":100.00,"expenses":0,"openInvoiceCount":2,"openInvoiceAmount":242.00,
          "openInvoices":[
            {"id":7,"number":"2026-0007","date":"2026-10-01","dueDate":"2026-10-15","totalInclVat":121.00,"overdue":true},
            {"id":9,"number":"2026-0009","date":"2026-11-19","dueDate":"2026-12-03","totalInclVat":121.00,"overdue":false}]},
         {"ownerId":2,"ownerName":"Pietersen","assetCount":1,"income":0,"expenses":0,"openInvoiceCount":0,"openInvoiceAmount":0}]
        """;

    private readonly FrontendFactory _factory;

    public OwnerOverviewOpenInvoicesTests(FrontendFactory factory) => _factory = factory;

    private async Task<string> GetReportsAsync(string culture)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c%3D{culture}%7Cuic%3D{culture}");
        using var response = await client.GetAsync(new Uri("/Reports?From=2026-01-01&To=2026-12-31", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData("nl-NL", "01-10-2026", "15-10-2026", "03-12-2026", "Vervallen", "Vervaldatum")]
    [InlineData("en-US", "10/1/2026", "10/15/2026", "12/3/2026", "Overdue", "Due date")]
    public async Task Open_invoices_show_under_their_owner_with_due_date_and_overdue_mark(string culture, string date, string dueOverdue, string dueOpen, string overdue, string dueHeader)
    {
        var html = await GetReportsAsync(culture);

        Assert.Contains("<a href=\"/Invoices/Details/7\">2026-0007</a>", html);
        Assert.Contains("<a href=\"/Invoices/Details/9\">2026-0009</a>", html);
        Assert.Contains($"<td>{date}</td>", html);
        Assert.Contains($"<td>{dueOverdue}</td>", html);
        Assert.Contains($"<td>{dueOpen}</td>", html);
        Assert.Contains($"<th>{dueHeader}</th>", html);
        Assert.Equal(1, CountOf(html, $"<span class=\"badge bg-danger\">{overdue}</span>"));
    }

    [Fact]
    public async Task The_overdue_mark_follows_the_api_flag_only()
    {
        var html = await GetReportsAsync("en-US");

        Assert.Contains("Overdue", Row(html, "2026-0007"));
        Assert.DoesNotContain("Overdue", Row(html, "2026-0009"));
    }

    [Fact]
    public async Task An_owner_without_open_invoices_gets_no_invoice_list()
    {
        var html = await GetReportsAsync("nl-NL");

        Assert.Equal(1, CountOf(html, "class=\"owner-open-invoices\""));
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string Row(string html, string number)
    {
        var at = html.IndexOf($">{number}</a>", StringComparison.Ordinal);
        Assert.True(at >= 0, number);
        var end = html.IndexOf("</tr>", at, StringComparison.Ordinal);
        return html[at..end];
    }

    public sealed class FrontendFactory : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Api:BaseUrl", "http://api.test/");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(o =>
                    {
                        o.DefaultScheme = TestAuthHandler.Name;
                        o.DefaultAuthenticateScheme = TestAuthHandler.Name;
                        o.DefaultChallengeScheme = TestAuthHandler.Name;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Name, _ => { });
                services.AddHttpClient<ApiClient>().ConfigurePrimaryHttpMessageHandler(() => new StubApi());
            });
        }
    }

    /// <summary>Signs in a staff member who may see the reports, without the API.</summary>
    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
            claims.AddRange(new[] { Permissions.ReadAll, Permissions.Reports }.Select(p => new Claim(Permissions.ClaimType, p)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Name, ClaimTypes.Name, ClaimTypes.Role));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Name)));
        }
    }

    /// <summary>The API as in the B23 contract: one owner with two open invoices (one overdue), one without.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.RequestUri!.AbsolutePath == "/api/reports/owner-overview" ? Overview : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
