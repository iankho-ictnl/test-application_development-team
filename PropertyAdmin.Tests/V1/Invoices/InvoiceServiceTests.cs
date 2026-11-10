using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Invoices;
using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Tests.V1.Invoices;

public sealed class InvoiceServiceTests
{
    private static readonly DateOnly InvoiceDate = new(2026, 3, 31);

    [Fact]
    public void VatSummary_rounds_vat_once_per_percentage_highest_first()
    {
        var summary = InvoiceTotals.VatSummary([(0.02m, 21m), (0.02m, 21m), (0.02m, 21m), (10m, 9m)]);

        Assert.Equal([21m, 9m], summary.Select(line => line.VatPercentage));
        Assert.Equal(0.06m, summary[0].Base);
        Assert.Equal(0.01m, summary[0].VatAmount);
        Assert.Equal(0.90m, summary[1].VatAmount);
        Assert.Equal(10.06m + 0.91m, InvoiceTotals.TotalInclVat([(0.02m, 21m), (0.02m, 21m), (0.02m, 21m), (10m, 9m)]));
    }

    [Fact]
    public async Task Create_numbers_per_year_copies_lines_and_never_invoices_twice()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAssetAsync(database.Context, "Owner");
        var activity = AddActivity(database.Context, asset, new DateOnly(2026, 3, 2), 200m);
        var expense = AddExpense(database.Context, asset, new DateOnly(2026, 3, 1), 100m, reinvoiceable: true);
        var notReinvoiceable = AddExpense(database.Context, asset, new DateOnly(2026, 3, 1), 50m, reinvoiceable: false);
        var later = AddActivity(database.Context, asset, new DateOnly(2026, 4, 15), 75m);
        await database.Context.SaveChangesAsync();
        var service = new InvoiceService(database.Context);

        var invoice = await service.CreateAsync(new InvoiceCreateModel(asset.OwnerId, InvoiceDate, 14), CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Invoices.Create.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Invoices.Create.Result(invoice));

        Assert.Equal("2026-0001", response.Number);
        Assert.Equal(new DateOnly(2026, 4, 14), response.DueDate);
        Assert.Equal(PropertyAdmin.Api.V1.InvoiceStatus.Open, response.Status);
        Assert.Equal("Owner", response.OwnerName);
        Assert.Equal(["01-03-2026 Owner house: Expense", "02-03-2026 Owner house: Activity"], response.Lines.Select(line => line.Description).Order());
        Assert.Equal(300m, response.TotalExclVat);
        Assert.Equal(63m, response.TotalVat);
        Assert.Equal(363m, response.TotalInclVat);
        Assert.Equal(21m, Assert.Single(response.VatSummary).VatPercentage);
        database.Context.ChangeTracker.Clear();
        Assert.Equal(invoice.Id, (await database.Context.Activities.FindAsync(activity.Id))!.InvoiceId);
        Assert.Equal(invoice.Id, (await database.Context.Expenses.FindAsync(expense.Id))!.InvoiceId);
        Assert.Null((await database.Context.Expenses.FindAsync(notReinvoiceable.Id))!.InvoiceId);
        Assert.Null((await database.Context.Activities.FindAsync(later.Id))!.InvoiceId);

        var nothingLeft = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new InvoiceCreateModel(asset.OwnerId, InvoiceDate, 14), CancellationToken.None));
        Assert.Equal("Er zijn geen te factureren activiteiten of kosten voor deze eigenaar.", nothingLeft.Message);

        var second = await service.CreateAsync(new InvoiceCreateModel(asset.OwnerId, new DateOnly(2026, 4, 30), 30), CancellationToken.None);
        Assert.Equal("2026-0002", second.Number);
        Assert.Equal(75m, second.TotalExclVat);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_owner()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new InvoiceService(database.Context);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(new InvoiceCreateModel(999, InvoiceDate, 14), CancellationToken.None));

        Assert.Equal("Eigenaar bestaat niet.", error.Message);
    }

    [Fact]
    public async Task GetUninvoiced_lists_open_activities_and_reinvoiceable_expenses_of_the_owner()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAssetAsync(database.Context, "Owner");
        var otherAsset = await SeedAssetAsync(database.Context, "Other");
        var activity = AddActivity(database.Context, asset, new DateOnly(2026, 3, 2), 200m);
        var expense = AddExpense(database.Context, asset, new DateOnly(2026, 3, 1), 100m, reinvoiceable: true);
        AddExpense(database.Context, asset, new DateOnly(2026, 3, 1), 50m, reinvoiceable: false);
        AddActivity(database.Context, otherAsset, new DateOnly(2026, 3, 2), 10m);
        await database.Context.SaveChangesAsync();
        var service = new InvoiceService(database.Context);

        var uninvoiced = await service.GetUninvoicedAsync(asset.OwnerId, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Invoices.GetUninvoiced.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Invoices.GetUninvoiced.Result(uninvoiced));

        Assert.Equal([activity.Id], response.Activities.Select(item => item.Id));
        Assert.Equal([expense.Id], response.Expenses.Select(item => item.Id));
        Assert.Equal(242m, response.Activities[0].AmountInclVat);
        Assert.Equal(121m, response.Expenses[0].AmountInclVat);
    }

    [Fact]
    public async Task MarkPaid_checks_existence_status_and_date()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var invoice = await SeedInvoiceAsync(database.Context);
        var service = new InvoiceService(database.Context);

        Assert.False(await service.MarkPaidAsync(invoice.Id + 100, InvoiceDate, CancellationToken.None));
        var early = await Assert.ThrowsAsync<BusinessRuleException>(() => service.MarkPaidAsync(invoice.Id, InvoiceDate.AddDays(-1), CancellationToken.None));
        Assert.Equal("Betaaldatum ligt voor de factuurdatum.", early.Message);
        Assert.True(await service.MarkPaidAsync(invoice.Id, InvoiceDate.AddDays(1), CancellationToken.None));
        var paid = await service.GetByIdAsync(invoice.Id, null, CancellationToken.None);
        Assert.Equal(InvoiceStatus.Paid, paid!.Status);
        Assert.Equal(InvoiceDate.AddDays(1), paid.PaidDate);
        var again = await Assert.ThrowsAsync<BusinessRuleException>(() => service.MarkPaidAsync(invoice.Id, InvoiceDate.AddDays(2), CancellationToken.None));
        Assert.Equal("Factuur is al betaald.", again.Message);
        var delete = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(invoice.Id, CancellationToken.None));
        Assert.Equal("Een betaalde factuur kan niet worden verwijderd.", delete.Message);
    }

    [Fact]
    public async Task Delete_releases_the_invoiced_activities_and_expenses()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var invoice = await SeedInvoiceAsync(database.Context);
        var service = new InvoiceService(database.Context);

        Assert.False(await service.DeleteAsync(invoice.Id + 100, CancellationToken.None));
        Assert.True(await service.DeleteAsync(invoice.Id, CancellationToken.None));

        Assert.Null(await service.GetByIdAsync(invoice.Id, null, CancellationToken.None));
        var ownerId = database.Context.Owners.Single().Id;
        var uninvoiced = await service.GetUninvoicedAsync(ownerId, CancellationToken.None);
        Assert.Single(uninvoiced.Activities);
        Assert.Single(uninvoiced.Expenses);
    }

    [Fact]
    public async Task GetAll_and_GetById_apply_owner_scope_and_filters()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var invoice = await SeedInvoiceAsync(database.Context);
        var other = await SeedAssetAsync(database.Context, "Other");
        await database.Context.SaveChangesAsync();
        var service = new InvoiceService(database.Context);

        var scoped = await service.GetAllAsync(invoice.OwnerId, null, null, CancellationToken.None);
        var paidOnly = await service.GetAllAsync(null, null, InvoiceStatus.Paid, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Invoices.GetAll.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Invoices.GetAll.Result(scoped));

        Assert.Equal([invoice.Id], response.Select(item => item.Id));
        Assert.Equal(363m, response[0].TotalInclVat);
        Assert.Equal(2, response[0].Lines.Count);
        Assert.Empty(paidOnly);
        Assert.Empty(await service.GetAllAsync(other.OwnerId, null, null, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(invoice.Id, other.OwnerId, CancellationToken.None));
        Assert.NotNull(await service.GetByIdAsync(invoice.Id, invoice.OwnerId, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAssetAsync(database.Context, "Owner");
        AddActivity(database.Context, asset, new DateOnly(2026, 3, 2), 200m);
        await database.Context.SaveChangesAsync();
        var service = new InvoiceService(database.Context);
        var handler = new PropertyAdmin.Logic.V1.Invoices.Create.CreateHandler(service);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAllAsync(null, null, null, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new PropertyAdmin.Logic.V1.Invoices.Create.Command(new InvoiceCreateModel(asset.OwnerId, InvoiceDate, 14)), cancellation.Token));
        Assert.Empty(database.Context.Invoices);
    }

    [Fact]
    public async Task GetPdf_renders_the_scoped_invoice_and_passes_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var invoice = await SeedInvoiceAsync(database.Context);
        var service = new InvoiceService(database.Context);
        var renderer = new PropertyAdmin.Logic.V1.Invoices.GetPdf.InvoicePdfRenderer(
            Microsoft.Extensions.Options.Options.Create(new PropertyAdmin.Logic.V1.Invoices.GetPdf.CompanyOptions { Name = "Company" }));
        var handler = new PropertyAdmin.Logic.V1.Invoices.GetPdf.GetPdfHandler(service, renderer);

        var result = await handler.HandleAsync(new PropertyAdmin.Logic.V1.Invoices.GetPdf.Query(invoice.Id, invoice.OwnerId), CancellationToken.None);
        Assert.True(result.Found);
        Assert.Equal($"Factuur-{invoice.Number}.pdf", result.FileName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(result.Content![..4]));
        Assert.False((await handler.HandleAsync(new PropertyAdmin.Logic.V1.Invoices.GetPdf.Query(invoice.Id, invoice.OwnerId + 100), CancellationToken.None)).Found);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new PropertyAdmin.Logic.V1.Invoices.GetPdf.Query(invoice.Id, null), cancellation.Token));
    }

    private static async Task<Asset> SeedAssetAsync(AppDbContext context, string ownerName)
    {
        var asset = new Asset { Owner = new Owner { Name = ownerName }, Name = $"{ownerName} house", Address = "Road 1", PostalCode = "1000 AA", City = "City" };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset;
    }

    private static Activity AddActivity(AppDbContext context, Asset asset, DateOnly date, decimal amount)
    {
        var activity = new Activity { AssetId = asset.Id, Date = date, Description = "Activity", PricingType = PricingType.Fixed, FixedPrice = amount, AmountExclVat = amount, VatRateName = "Hoog", VatPercentage = 21m };
        context.Activities.Add(activity);
        return activity;
    }

    private static Expense AddExpense(AppDbContext context, Asset asset, DateOnly date, decimal amount, bool reinvoiceable)
    {
        var expense = new Expense { AssetId = asset.Id, Date = date, Description = "Expense", AmountExclVat = amount, VatRateName = "Hoog", VatPercentage = 21m, Reinvoiceable = reinvoiceable };
        context.Expenses.Add(expense);
        return expense;
    }

    private static async Task<InvoiceModel> SeedInvoiceAsync(AppDbContext context)
    {
        var asset = await SeedAssetAsync(context, "Owner");
        AddActivity(context, asset, new DateOnly(2026, 3, 2), 200m);
        AddExpense(context, asset, new DateOnly(2026, 3, 1), 100m, reinvoiceable: true);
        await context.SaveChangesAsync();
        return await new InvoiceService(context).CreateAsync(new InvoiceCreateModel(asset.OwnerId, InvoiceDate, 14), CancellationToken.None);
    }
}
