using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PropertyAdmin.Data;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>
/// Hosts the real API in memory (D-011) on an in-memory SQLite database, so the characterization
/// tests (T-042) fix the behaviour before the D-026 restructuring. Test data is fictitious.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "admin.test";
    public const string Password = "Test-Password-123!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Dictionary<string, string> _tokens = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _counter;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=not-used",
            ["Jwt:Key"] = "characterization-tests-signing-key-0123456789abcdef",
            ["Seed:AdminUserName"] = AdminUserName,
            ["Seed:AdminEmail"] = "admin.test@example.test",
            ["Seed:AdminPassword"] = Password,
            ["Company:Name"] = "Testbeheer BV",
        };
        foreach (var (key, value) in settings) builder.UseSetting(key, value);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });
    }

    public string Unique(string prefix) => $"{prefix}-{Interlocked.Increment(ref _counter)}";

    public HttpClient Anonymous() => CreateClient();

    public HttpClient WithToken(string token)
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    /// <summary>A client signed in as a user with this seeded role (one user per role and owner).</summary>
    public async Task<HttpClient> As(string role, int? ownerId = null)
    {
        var userName = role == "Admin" && ownerId is null ? AdminUserName : $"{role.ToLowerInvariant()}{ownerId}.test";
        await _lock.WaitAsync();
        try
        {
            if (!_tokens.TryGetValue(userName, out var token))
            {
                if (userName != AdminUserName) await CreateUserAsync(userName, role, ownerId);
                token = await LoginAsync(userName, Password);
                _tokens[userName] = token;
            }
            return WithToken(token);
        }
        finally { _lock.Release(); }
    }

    public async Task CreateUserAsync(string userName, string? role, int? ownerId = null)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.test", EmailConfirmed = true, OwnerId = ownerId };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        if (role is not null) await users.AddToRoleAsync(user, role);
    }

    public async Task<string> LoginAsync(string userName, string password)
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "API characterization";
}
