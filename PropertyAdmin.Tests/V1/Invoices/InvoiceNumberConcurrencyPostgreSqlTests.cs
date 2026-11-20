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

    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_db.GetConnectionString()).Options);

    private static void AddActivity(AppDbContext context, int assetId) => context.Activities.Add(new Activity
    {
        AssetId = assetId, Date = new DateOnly(2026, 3, 2), Description = "Onderhoud", PricingType = PricingType.Fixed,
        FixedPrice = 100m, AmountExclVat = 100m, VatRateName = "Hoog", VatPercentage = 21m,
    });
}
