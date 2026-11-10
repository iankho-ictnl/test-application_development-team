using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Auth;
using PropertyAdmin.Logic.V1.Auth.Models;

namespace PropertyAdmin.Tests.V1;

/// <summary>ASP.NET Core Identity on an in-memory SQLite database, for the Auth, Users and Roles unit tests.</summary>
internal sealed class IdentityTestHost : IAsyncDisposable
{
    public const string Password = "Test-Password-123!";

    private readonly SqliteConnection connection;
    private readonly ServiceProvider provider;
    private readonly AsyncServiceScope scope;

    private IdentityTestHost(SqliteConnection connection, ServiceProvider provider)
    {
        this.connection = connection;
        this.provider = provider;
        scope = provider.CreateAsyncScope();
    }

    public FakeTokenIssuer Tokens { get; } = new();

    public IServiceProvider Services => scope.ServiceProvider;

    public AppDbContext Db => Services.GetRequiredService<AppDbContext>();

    public UserManager<ApplicationUser> Users => Services.GetRequiredService<UserManager<ApplicationUser>>();

    public RoleManager<IdentityRole> Roles => Services.GetRequiredService<RoleManager<IdentityRole>>();

    public static async Task<IdentityTestHost> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 10;
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddDataStores()
            .AddDefaultTokenProviders();
        var host = new IdentityTestHost(connection, services.BuildServiceProvider());
        await host.Db.Database.EnsureCreatedAsync();
        return host;
    }

    public async Task<IdentityRole> CreateRoleAsync(string name, params string[] permissions)
    {
        var role = new IdentityRole(name);
        await Roles.CreateAsync(role);
        foreach (var permission in permissions)
            await Roles.AddClaimAsync(role, new System.Security.Claims.Claim(PropertyAdmin.Contracts.Permissions.ClaimType, permission));
        return role;
    }

    public async Task<ApplicationUser> CreateUserAsync(string userName, int? ownerId = null, params string[] roles)
    {
        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@example.test", EmailConfirmed = true, OwnerId = ownerId };
        var result = await Users.CreateAsync(user, Password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        if (roles.Length > 0) await Users.AddToRolesAsync(user, roles);
        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
        await connection.DisposeAsync();
    }
}

internal sealed class FakeTokenIssuer : ITokenIssuer
{
    public TokenSubjectModel? LastSubject { get; private set; }

    public AccessTokenModel CreateAccessToken(TokenSubjectModel subject)
    {
        LastSubject = subject;
        return new AccessTokenModel("access-token", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
    }

    public string CreateChallengeToken(string userId, string securityStamp) => "challenge:" + userId;

    public string? ConsumeChallengeToken(string token) =>
        token.StartsWith("challenge:", StringComparison.Ordinal) ? token["challenge:".Length..] : null;
}
