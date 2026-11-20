using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Invoices;
using PropertyAdmin.Logic.V1.Invoices.Models;
using PropertyAdmin.Tests.Demo;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace PropertyAdmin.Tests.V1.Invoices;

/// <summary>
/// TC-REQ7-1 (REQ-007, D-056, Q-118 b, T-181): on a real PostgreSQL 18 (D-031, Testcontainers, same image as the demo
/// test and docker-compose.yml) a delete of the most recent invoice and a create at the same moment never leave a gap
/// or a duplicate in the invoice numbers of the year. Two separate DbContexts, so two connections, as two API requests.
/// Data is fictitious; the database password is generated per run.
/// </summary>
[Trait("Category", "PostgreSql")]
public sealed class InvoiceNumberConcurrencyPostgreSqlTests(ITestOutputHelper output) : IAsyncLifetime
{
    private const int Rounds = 25;
    private static readonly DateOnly InvoiceDate = new(2026, 3, 31);

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder(DemoStack.PostgresImage)
        .WithDatabase("propertyadmin_concurrency")
        .WithUsername("concurrency")
        .WithPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(24)))
        .Build();

    public async Task InitializeAsync()
    {
        try
        {
            await _db.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("the PostgreSQL tests need Docker Desktop", ex);
        }

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Delete_of_the_newest_invoice_and_a_create_at_the_same_moment_leave_no_gap_in_the_numbers_TC_REQ7_1()
    {
        int assetId;
        int ownerId;
        await using (var setup = NewContext())
        {
            var asset = new Asset { Owner = new Owner { Name = "Gelijktijdig BV" }, Name = "Testpand", Street = "Proefweg", PostalCode = "1000 AA", City = "Proefdorp" };
            setup.Assets.Add(asset);
            await setup.SaveChangesAsync();
            assetId = asset.Id;
            ownerId = asset.OwnerId;
            AddActivity(setup, assetId);
            await setup.SaveChangesAsync();
            await new InvoiceService(setup).CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
        }

        var deleted = 0;
        var refused = 0;
        for (var round = 1; round <= Rounds; round++)
        {
            int newestId;
            await using (var prepare = NewContext())
            {
                AddActivity(prepare, assetId);
                await prepare.SaveChangesAsync();
                newestId = (await prepare.Invoices.AsNoTracking().OrderByDescending(invoice => invoice.Number).FirstAsync()).Id;
            }

            await using var deleteContext = NewContext();
            await using var createContext = NewContext();
            await deleteContext.Database.OpenConnectionAsync();
            await createContext.Database.OpenConnectionAsync();
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var delete = Task.Run(async () =>
            {
                await start.Task;
                try
                {
                    return await new InvoiceService(deleteContext).DeleteAsync(newestId, CancellationToken.None) ? "deleted" : "not found";
                }
                catch (BusinessRuleException ex)
                {
                    return ex.Message;
                }
            });
            var create = Task.Run(async () =>
            {
                await start.Task;
                return await new InvoiceService(createContext).CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
            });
            start.SetResult();
            var deleteResult = await delete;
            var created = await create;

            if (deleteResult == "deleted") deleted++;
            else Assert.Equal(InvoiceService.OnlyMostRecentDeletable, deleteResult);
            if (deleteResult != "deleted") refused++;

            await using var check = NewContext();
            var numbers = await check.Invoices.AsNoTracking().Where(invoice => invoice.Number.StartsWith("2026-")).Select(invoice => invoice.Number).ToListAsync();
            var expected = Enumerable.Range(1, numbers.Count).Select(sequence => string.Create(CultureInfo.InvariantCulture, $"2026-{sequence:0000}"));
            Assert.True(expected.SequenceEqual(numbers.Order(StringComparer.Ordinal)),
                $"round {round}: delete {deleteResult}, created {created.Number}, numbers now {string.Join(", ", numbers.Order(StringComparer.Ordinal))}");
            Assert.Equal(numbers.Max(StringComparer.Ordinal), created.Number);
            Assert.Equal(0, await check.Activities.CountAsync(activity => activity.InvoiceId == null));
        }

        output.WriteLine($"{Rounds} rounds: delete first (number reused) {deleted}, create first (delete refused) {refused}");
    }

    /// <summary>
    /// TC-REQ7-2 (REQ-007, D-056, code review of #24 Should fix, T-163): two deletes of the same newest invoice at the same
    /// moment on two connections: one deletes it (true, the API's 204), the other finds it gone (false, the API's 404), never
    /// an exception (the API's 500). The invoice is gone and its activity is uninvoiced again.
    /// </summary>
    [Fact]
    public async Task Two_deletes_of_the_same_invoice_at_the_same_moment_give_one_deleted_and_one_not_found_TC_REQ7_2()
    {
        var (assetId, ownerId) = await AddOwnerWithAssetAsync("Dubbel Verwijderen BV");
        for (var round = 1; round <= 10; round++)
        {
            InvoiceModel invoice;
            await using (var prepare = NewContext())
            {
                AddActivity(prepare, assetId);
                await prepare.SaveChangesAsync();
                invoice = await new InvoiceService(prepare).CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
            }

            await using var first = NewContext();
            await using var second = NewContext();
            await first.Database.OpenConnectionAsync();
            await second.Database.OpenConnectionAsync();
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var deleteFirst = Task.Run(async () => { await start.Task; return await new InvoiceService(first).DeleteAsync(invoice.Id, CancellationToken.None); });
            var deleteSecond = Task.Run(async () => { await start.Task; return await new InvoiceService(second).DeleteAsync(invoice.Id, CancellationToken.None); });
            start.SetResult();
            var results = await Task.WhenAll(deleteFirst, deleteSecond);

            Assert.True(results.Count(deleted => deleted) == 1, $"round {round}: results {string.Join(", ", results)}");
            await using var check = NewContext();
            Assert.False(await check.Invoices.AnyAsync(item => item.Id == invoice.Id));
            Assert.Equal(1, await check.Activities.CountAsync(activity => activity.InvoiceId == null));
            check.Activities.RemoveRange(check.Activities.Where(activity => activity.InvoiceId == null));
            await check.SaveChangesAsync();
        }
    }

    /// <summary>
    /// TC-REQ7-3 (REQ-007, D-056, T-163): five invoices for five owners created at the same moment on five connections get the
    /// consecutive numbers 2026-0001 to 2026-0005, each once.
    /// </summary>
    [Fact]
    public async Task Invoices_created_at_the_same_moment_get_consecutive_numbers_TC_REQ7_3()
    {
        var owners = new List<int>();
        for (var index = 1; index <= 5; index++)
        {
            var (assetId, ownerId) = await AddOwnerWithAssetAsync($"Tegelijk {index} BV");
            await using var setup = NewContext();
            AddActivity(setup, assetId);
            await setup.SaveChangesAsync();
            owners.Add(ownerId);
        }

        var contexts = owners.Select(_ => NewContext()).ToList();
        try
        {
            foreach (var context in contexts) await context.Database.OpenConnectionAsync();
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var creates = owners.Select((ownerId, index) => Task.Run(async () =>
            {
                await start.Task;
                return await new InvoiceService(contexts[index]).CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
            })).ToList();
            start.SetResult();
            var created = await Task.WhenAll(creates);

            Assert.Equal(["2026-0001", "2026-0002", "2026-0003", "2026-0004", "2026-0005"], created.Select(invoice => invoice.Number).Order(StringComparer.Ordinal));
            Assert.Equal(owners.Order(), created.Select(invoice => invoice.OwnerId).Order());
        }
        finally
        {
            foreach (var context in contexts) await context.DisposeAsync();
        }
    }

    /// <summary>
    /// TC-REQ7-4 (REQ-007, D-056, T-163): when the most recent invoice is deleted, the next invoice gets its number again.
    /// </summary>
    [Fact]
    public async Task A_deleted_most_recent_number_is_reused_TC_REQ7_4()
    {
        var (assetId, ownerId) = await AddOwnerWithAssetAsync("Hergebruik BV");
        await using var context = NewContext();
        var service = new InvoiceService(context);
        AddActivity(context, assetId);
        await context.SaveChangesAsync();
        await service.CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
        AddActivity(context, assetId);
        await context.SaveChangesAsync();
        var newest = await service.CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);
        Assert.Equal("2026-0002", newest.Number);

        Assert.True(await service.DeleteAsync(newest.Id, CancellationToken.None));
        var again = await service.CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);

        Assert.Equal("2026-0002", again.Number);
    }

    /// <summary>
    /// TC-REQ7-5 (REQ-018, REQ-007, T-163): after the real migrations every money column is numeric(18,2) in PostgreSQL, and an
    /// invoice line amount is stored and read back to the cent.
    /// </summary>
    [Fact]
    public async Task Money_columns_are_numeric_18_2_and_keep_cents_TC_REQ7_5()
    {
        string[] columns = ["Activities.AmountExclVat", "Activities.FixedPrice", "Activities.HourlyRate", "Expenses.AmountExclVat", "InvoiceLines.AmountExclVat"];
        await using var context = NewContext();
        await context.Database.OpenConnectionAsync();
        foreach (var column in columns)
        {
            var parts = column.Split('.');
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT data_type || '(' || numeric_precision || ',' || numeric_scale || ')' FROM information_schema.columns WHERE table_name = @table AND column_name = @column";
            var table = command.CreateParameter();
            table.ParameterName = "table";
            table.Value = parts[0];
            command.Parameters.Add(table);
            var name = command.CreateParameter();
            name.ParameterName = "column";
            name.Value = parts[1];
            command.Parameters.Add(name);
            Assert.Equal("numeric(18,2)", (string?)await command.ExecuteScalarAsync());
        }

        var (assetId, ownerId) = await AddOwnerWithAssetAsync("Centen BV");
        context.Activities.Add(new Activity
        {
            AssetId = assetId, Date = new DateOnly(2026, 3, 2), Description = "Groot onderhoud", PricingType = PricingType.Fixed,
            FixedPrice = 1234567890123456.78m, AmountExclVat = 1234567890123456.78m, VatRateName = "Hoog", VatPercentage = 21m,
        });
        await context.SaveChangesAsync();
        var invoice = await new InvoiceService(context).CreateAsync(new InvoiceCreateModel(ownerId, InvoiceDate, InvoiceService.PaymentTermDays), CancellationToken.None);

        await using var check = NewContext();
        Assert.Equal(1234567890123456.78m, (await check.InvoiceLines.AsNoTracking().SingleAsync(line => line.InvoiceId == invoice.Id)).AmountExclVat);
    }

    private async Task<(int AssetId, int OwnerId)> AddOwnerWithAssetAsync(string ownerName)
    {
        await using var setup = NewContext();
        var asset = new Asset { Owner = new Owner { Name = ownerName }, Name = "Testpand", Street = "Proefweg", PostalCode = "1000 AA", City = "Proefdorp" };
        setup.Assets.Add(asset);
        await setup.SaveChangesAsync();
        return (asset.Id, asset.OwnerId);
    }

    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_db.GetConnectionString()).Options);

    private static void AddActivity(AppDbContext context, int assetId) => context.Activities.Add(new Activity
    {
        AssetId = assetId, Date = new DateOnly(2026, 3, 2), Description = "Onderhoud", PricingType = PricingType.Fixed,
        FixedPrice = 100m, AmountExclVat = 100m, VatRateName = "Hoog", VatPercentage = 21m,
    });
}
