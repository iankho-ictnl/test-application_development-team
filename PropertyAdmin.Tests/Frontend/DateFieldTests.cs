extern alias Frontend;

using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
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

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// BUG-8 (REQ-004, REQ-005, REQ-007, REQ-021): the date fields are pre-filled with today as an HTML date
/// field (type="date", value yyyy-MM-dd, shown by the browser as dd-mm-jjjj in Dutch), in nl-NL and en-US.
/// </summary>
public sealed class DateFieldTests : IClassFixture<DateFieldTests.FrontendFactory>
{
    private readonly FrontendFactory _factory;

    public DateFieldTests(FrontendFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Expenses/Edit", "Input.Date", "nl-NL")]
    [InlineData("/Expenses/Edit", "Input.Date", "en-US")]
    [InlineData("/Activities/Edit", "Input.Date", "nl-NL")]
    [InlineData("/Invoices/Create?ownerId=1", "Date", "nl-NL")]
    [InlineData("/VatRates", "Input.ValidFrom", "nl-NL")]
    public async Task Date_field_is_prefilled_with_today_as_html_date(string path, string field, string culture)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c%3D{culture}%7Cuic%3D{culture}");

        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var input = Regex.Match(html, $"<input[^>]*name=\"{Regex.Escape(field)}\"[^>]*>");
        Assert.True(input.Success, $"No input {field} on {path}.");
        Assert.Contains("type=\"date\"", input.Value);
        Assert.Contains($"value=\"{DateTime.Today:yyyy-MM-dd}\"", input.Value);
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

    /// <summary>Signs in a staff member with the permissions of these pages, without the API.</summary>
    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
            claims.AddRange(new[] { Permissions.ReadAll, Permissions.ExpensesActivities, Permissions.Invoices, Permissions.VatRates }
                .Select(p => new Claim(Permissions.ClaimType, p)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Name, ClaimTypes.Name, ClaimTypes.Role));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Name)));
        }
    }

    /// <summary>The API: empty lists, and one uninvoiced activity for owner 1 so the invoice form shows.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        private const string Uninvoiced = """{"activities":[{"id":1,"assetId":1,"assetName":"A","date":"2026-11-01","kind":0,"description":"d","pricingType":0,"hours":1,"hourlyRate":100,"fixedPrice":null,"amountExclVat":100,"vatRateName":"Hoog","vatPercentage":21,"vatAmount":21,"amountInclVat":121,"invoiceId":null}],"expenses":[]}""";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.RequestUri!.AbsolutePath.StartsWith("/api/invoices/uninvoiced/", StringComparison.Ordinal) ? Uninvoiced : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
