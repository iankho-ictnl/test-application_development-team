extern alias Frontend;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using FrontendApiClient = Frontend::test_application_for_development_team.Services.ApiClient;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>
/// Hosts the admin frontend in memory (D-030) with its API client routed to its own in-memory
/// API (<see cref="ApiFactory"/>, own SQLite database), so the screens are regression-tested with a
/// real sign-in on every change (REQ-030). Test data is fictitious.
/// </summary>
public sealed class FrontendFactory : WebApplicationFactory<Frontend::Program>
{
    public static readonly string[] Roles = ["Admin", "Manager", "Staff", "Finance", "Viewer", "Owner"];

    private readonly SemaphoreSlim _usersLock = new(1, 1);
    private readonly SemaphoreSlim _dataLock = new(1, 1);
    private readonly Dictionary<string, string> _userByRole = [];
    private ScreenData? _data;

    public ApiFactory Api { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: the cookies are Secure as in production, so the browser uses https.
        builder.UseEnvironment("Testing");
        builder.UseSetting("Api:BaseUrl", "http://api.test/");
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<FrontendApiClient>().ConfigurePrimaryHttpMessageHandler(() => Api.Server.CreateHandler());
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    private int _lastClient;

    /// <summary>
    /// A new browser: own cookies, no automatic redirects, and its own client IP address (test host only), so the
    /// sign-in rate limit (D-044, per client IP) counts each test browser apart as it counts real users apart.
    /// </summary>
    public HttpClient NewBrowser(IPAddress? clientIp = null)
    {
        var n = Interlocked.Increment(ref _lastClient);
        clientIp ??= new IPAddress([10, 77, (byte)(n / 250), (byte)(n % 250 + 1)]);
        var client = CreateDefaultClient(new Uri("https://localhost"), new CookieContainerHandler(), new TestClientIpHandler(clientIp));
        return client;
    }

    /// <summary>Carries the browser's client IP address to the in-memory frontend in a test-only header.</summary>
    private sealed class TestClientIpHandler(IPAddress ip) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove(TestClientIpStartupFilter.Header);
            request.Headers.Add(TestClientIpStartupFilter.Header, ip.ToString());
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>Test host only: sets the connection's remote address from the test header, before the app's own middleware.</summary>
    private sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public const string Header = "X-Test-Client-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (IPAddress.TryParse(context.Request.Headers[Header].ToString(), out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                    context.Request.Headers.Remove(Header);
                }
                await nextMiddleware();
            });
            next(app);
        };
    }

    /// <summary>Signs in through the Login page and returns the browser with the sign-in cookie.</summary>
    public async Task<HttpClient> SignIn(string userName, string password = ApiFactory.Password)
    {
        var browser = NewBrowser();
        var response = await browser.Submit("/Account/Login", new() { ["UserName"] = userName, ["Password"] = password });
        Browser.ExpectRedirect(response, "/");
        return browser;
    }

    /// <summary>A new browser signed in as this test's user of the seeded role (never the seed admin, whose API token other tests share).</summary>
    public async Task<HttpClient> SignInAs(string role) => await SignIn(await UserFor(role));

    public async Task<string> UserFor(string role)
    {
        int? ownerId = role == "Owner" ? (await DataAsync()).OwnerId : null;
        await _usersLock.WaitAsync();
        try
        {
            if (!_userByRole.TryGetValue(role, out var userName))
            {
                userName = $"screen.{role.ToLowerInvariant()}";
                await Api.CreateUserAsync(userName, role, ownerId);
                _userByRole[role] = userName;
            }
            return userName;
        }
        finally { _usersLock.Release(); }
    }

    /// <summary>One owner with an asset, an invoiced expense and activity, shared read-only by the tests.</summary>
    public async Task<ScreenData> DataAsync()
    {
        await _dataLock.WaitAsync();
        try { return _data ??= await CreateDataAsync(); }
        finally { _dataLock.Release(); }
    }

    /// <summary>Own data for a test: owner, asset, expense (100 + 21 %), activity (200 + 21 %), invoice.</summary>
    public async Task<ScreenData> CreateDataAsync()
    {
        var admin = await Api.As("Admin");
        var ownerName = Api.Unique("Schermeigenaar");
        var ownerId = await admin.CreateOwner(ownerName);
        var assetName = Api.Unique("Schermpand");
        var assetId = await admin.CreateAsset(ownerId, assetName);
        await admin.CreateExpense(assetId);
        await admin.CreateActivity(assetId);
        var invoice = await Http.Expect(await admin.PostAsJsonAsync("/api/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        return new(ownerId, ownerName, assetId, assetName, invoice.GetProperty("id").GetInt32(), invoice.GetProperty("number").GetString()!);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Api.Dispose();
    }
}

public sealed record ScreenData(int OwnerId, string OwnerName, int AssetId, string AssetName, int InvoiceId, string InvoiceNumber);

[CollectionDefinition(Name)]
public sealed class FrontendCollection : ICollectionFixture<FrontendFactory>
{
    public const string Name = "Frontend screens regression";
}
