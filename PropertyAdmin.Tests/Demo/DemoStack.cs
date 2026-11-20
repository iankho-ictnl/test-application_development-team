extern alias Frontend;

using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using ApiClient = Frontend::test_application_for_development_team.Services.ApiClient;
using FrontendProgram = Frontend::Program;

namespace PropertyAdmin.Tests.Demo;

/// <summary>
/// D-047 (rule 15): the whole app for a sprint demo test. A throwaway PostgreSQL 18 container (same tag as
/// docker-compose.yml, T-115 C3), the real API host on it (normal migrations and normal seed admin), and the real
/// frontend host whose ApiClient calls that API host. Every password is generated per run and handed over in
/// memory only (C4); the Testcontainers resource reaper stays on (C7). Data is fictitious (C5).
/// </summary>
public sealed class DemoStack : IAsyncLifetime
{
    public const string PostgresImage = "postgres:18-trixie";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase("propertyadmin_demo")
        .WithUsername("demo")
        .WithPassword(Secret())
        .Build();

    private DemoApi? _api;
    private DemoFrontend? _frontend;

    public string AdminUserName { get; } = "demo.admin";
    internal string AdminPassword { get; } = Secret() + "Aa1!";

    public async Task InitializeAsync()
    {
        try
        {
            await _db.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("the demo test needs Docker Desktop", ex);
        }

        _api = new DemoApi(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _db.GetConnectionString(),
            ["Jwt:Key"] = Secret() + Secret(),
            ["Seed:AdminUserName"] = AdminUserName,
            ["Seed:AdminEmail"] = "demo.admin@example.test",
            ["Seed:AdminPassword"] = AdminPassword,
            ["Company:Name"] = "Demobeheer BV",
            ["Company:Address"] = "Demostraat 1",
            ["Company:PostalCode"] = "1234 AB",
            ["Company:City"] = "Demodorp",
        });
        _ = _api.Server; // starts the API: migrations and seed run now
        _frontend = new DemoFrontend(_api);
    }

    /// <summary>A new browser session (own cookies) on the frontend, over HTTPS as behind the proxy.</summary>
    public Browser NewBrowser() => new(_frontend!.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost/"),
        AllowAutoRedirect = true,
        HandleCookies = true,
    }));

    public async Task DisposeAsync()
    {
        _frontend?.Dispose();
        _api?.Dispose();
        await _db.DisposeAsync();
    }

    private static string Secret() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24));

    private sealed class DemoApi(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        }
    }

    private sealed class DemoFrontend(DemoApi api) : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var settings = new Dictionary<string, string?> { ["Api:BaseUrl"] = "http://localhost/" };
            builder.UseEnvironment("Testing");
            foreach (var (key, value) in settings) builder.UseSetting(key, value);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<ApiClient>().ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler()));
        }
    }
}

/// <summary>Posts the real Razor forms with their antiforgery token, as a browser without JavaScript does.</summary>
public sealed partial class Browser(HttpClient client) : IDisposable
{
    public string Html { get; private set; } = "";
    public HttpStatusCode Status { get; private set; }
    public string Path { get; private set; } = "";

    public async Task<string> GetAsync(string url)
    {
        await Store(await client.GetAsync(url));
        return Html;
    }

    /// <summary>Posts a form of the page at <paramref name="pageUrl"/> to <paramref name="action"/> (default: the page itself).</summary>
    public async Task<string> PostFormAsync(string pageUrl, IDictionary<string, string> fields, string? action = null)
    {
        await GetAsync(pageUrl);
        var token = TokenRegex().Match(Html);
        Assert.True(token.Success, $"no antiforgery token on {pageUrl}");
        var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value) };
        await Store(await client.PostAsync(action ?? pageUrl, new FormUrlEncodedContent(form)));
        return Html;
    }

    public async Task<byte[]> GetBytesAsync(string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task Store(HttpResponseMessage response)
    {
        Status = response.StatusCode;
        Path = response.RequestMessage!.RequestUri!.PathAndQuery;
        Html = await response.Content.ReadAsStringAsync();
    }

    public void Dispose() => client.Dispose();

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
}

[CollectionDefinition(Name)]
public sealed class DemoCollection : ICollectionFixture<DemoStack>
{
    public const string Name = "Sprint demo";
}
