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

namespace PropertyAdmin.Tests.Frontend.Owners;

/// <summary>
/// REQ-002 (US-2.1a, US-2.1b), REQ-011, T-102: the owner list shows street with house number and city; the form
/// shows the seven address and company fields with Dutch and English labels, KvK and BTW marked optional.
/// The API is a test double (B13 is not built yet).
/// </summary>
public sealed class OwnerPagesRenderTests : IClassFixture<OwnerPagesRenderTests.FrontendFactory>
{
    private const string Owner = """{"id":1,"name":"Jansen","email":"j@example.nl","phone":"06","street":"Damstraat","houseNumber":"12A","postalCode":"1012 JS","city":"Amsterdam","country":"Nederland","kvkNumber":"12345678","vatNumber":"NL123456789B01"}""";
    private readonly FrontendFactory _factory;

    public OwnerPagesRenderTests(FrontendFactory factory) => _factory = factory;

    private async Task<string> GetAsync(string path, string culture)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c%3D{culture}%7Cuic%3D{culture}");
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task List_shows_street_with_house_number_and_city()
    {
        var html = await GetAsync("/Owners", "nl-NL");

        Assert.Contains("<td>Damstraat 12A, Amsterdam</td>", html);
    }

    [Theory]
    [InlineData("nl-NL", "Straat", "Huisnummer", "Land", "KvK-nummer (optioneel)", "Btw-nummer (optioneel)")]
    [InlineData("en-US", "Street", "House number", "Country", "KvK number (optional)", "VAT number (optional)")]
    public async Task Edit_form_has_the_fields_with_labels_and_values(string culture, string street, string house, string country, string kvk, string vat)
    {
        var html = await GetAsync("/Owners/Edit/1", culture);

        foreach (var label in new[] { street, house, country, kvk, vat })
            Assert.Contains($">{label}</label>", html);
        foreach (var (field, value) in new[] { ("Street", "Damstraat"), ("HouseNumber", "12A"), ("PostalCode", "1012 JS"), ("City", "Amsterdam"), ("Country", "Nederland"), ("KvkNumber", "12345678"), ("VatNumber", "NL123456789B01") })
            Assert.Matches($"<input[^>]*name=\"Input.{field}\"[^>]*value=\"{value}\"", html);
    }

    [Fact]
    public async Task Details_show_the_full_address_and_the_kvk_and_btw_numbers()
    {
        var html = await GetAsync("/Owners/Details/1", "nl-NL");

        Assert.Contains("Damstraat 12A", html);
        Assert.Contains("1012 JS Amsterdam", html);
        Assert.Contains("Nederland", html);
        Assert.Contains("KvK-nummer: 12345678", html);
        Assert.Contains("Btw-nummer: NL123456789B01", html);
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

    /// <summary>Signs in a staff member who may read and edit owners, without the API.</summary>
    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
            claims.AddRange(new[] { Permissions.ReadAll, Permissions.OwnersAssets }.Select(p => new Claim(Permissions.ClaimType, p)));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Name, ClaimTypes.Name, ClaimTypes.Role));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Name)));
        }
    }

    /// <summary>The API as in the interface contract: one owner, no assets or invoices.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.RequestUri!.AbsolutePath switch
            {
                "/api/owners" => $"[{Owner}]",
                "/api/owners/1" => Owner,
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
