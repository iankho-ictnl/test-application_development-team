using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;

namespace PropertyAdmin.Tests.V1;

internal sealed class MasterDataTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private MasterDataTestDatabase(SqliteConnection connection, AppDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<MasterDataTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new MasterDataTestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}