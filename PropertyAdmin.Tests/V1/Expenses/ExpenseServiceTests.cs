using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Expenses;
using PropertyAdmin.Logic.V1.Expenses.Models;
using PropertyAdmin.Logic.V1.VatRates;
using ExpensesApi = PropertyAdmin.Api.V1.Expenses;
using ExpensesLogic = PropertyAdmin.Logic.V1.Expenses;

namespace PropertyAdmin.Tests.V1.Expenses;

public sealed class ExpenseServiceTests
{
    private const string RateName = "TestHoog";

    [Fact]
    public async Task Create_rounds_amount_snapshots_vat_and_maps_response()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);

        var expense = await service.CreateAsync(
            new ExpenseSaveModel(asset.Id, new DateOnly(2026, 3, 1), "  Lamp  ", "Onderhoud", 10.005m, RateName, true),
            CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Expenses.Create.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Expenses.Create.Result(expense));

        Assert.Equal("Lamp", response.Description);
        Assert.Equal(asset.Name, response.AssetName);
        Assert.Equal(10.01m, response.AmountExclVat);
        Assert.Equal(21m, response.VatPercentage);
        Assert.Equal(2.10m, response.VatAmount);
        Assert.Equal(12.11m, response.AmountInclVat);
        Assert.True(response.Reinvoiceable);
        Assert.Null(response.InvoiceId);
    }

    [Fact]
    public async Task Create_rejects_unknown_asset_and_vat_rate_without_a_valid_period()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);

        var unknownAsset = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            new ExpenseSaveModel(asset.Id + 100, new DateOnly(2026, 3, 1), "Lamp", null, 10m, RateName, false), CancellationToken.None));
        var noRate = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            new ExpenseSaveModel(asset.Id, new DateOnly(2019, 1, 1), "Lamp", null, 10m, RateName, false), CancellationToken.None));

        Assert.Equal("Object bestaat niet.", unknownAsset.Message);
        Assert.StartsWith($"Er is geen btw-tarief '{RateName}'", noRate.Message);
    }

    [Fact]
    public async Task Update_and_delete_reject_an_invoiced_expense()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var invoice = new Invoice { Number = "2026-0001", OwnerId = asset.OwnerId, Date = new DateOnly(2026, 4, 1), DueDate = new DateOnly(2026, 5, 1) };
        database.Context.Invoices.Add(invoice);
        await database.Context.SaveChangesAsync();
        var expense = new Expense { AssetId = asset.Id, Date = new DateOnly(2026, 3, 1), Description = "Lamp", VatRateName = RateName, InvoiceId = invoice.Id };
        database.Context.Expenses.Add(expense);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(
            expense.Id, new ExpenseSaveModel(asset.Id, new DateOnly(2026, 3, 1), "Other", null, 1m, RateName, false), CancellationToken.None));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(expense.Id, CancellationToken.None));
        Assert.Null(await service.UpdateAsync(
            expense.Id + 100, new ExpenseSaveModel(asset.Id, new DateOnly(2026, 3, 1), "Other", null, 1m, RateName, false), CancellationToken.None));
        Assert.False(await service.DeleteAsync(expense.Id + 100, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_applies_owner_scope_and_filters_newest_first()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var otherAsset = await SeedAsync(database.Context, "Other", seedRate: false);
        var service = CreateService(database.Context);
        var older = await service.CreateAsync(new ExpenseSaveModel(asset.Id, new DateOnly(2026, 2, 1), "Older", null, 1m, RateName, false), CancellationToken.None);
        var newer = await service.CreateAsync(new ExpenseSaveModel(asset.Id, new DateOnly(2026, 3, 1), "Newer", null, 1m, RateName, false), CancellationToken.None);
        await service.CreateAsync(new ExpenseSaveModel(otherAsset.Id, new DateOnly(2026, 3, 1), "Other", null, 1m, RateName, false), CancellationToken.None);

        var scoped = await service.GetAllAsync(asset.OwnerId, null, null, null, CancellationToken.None);
        var fromMarch = await service.GetAllAsync(null, asset.Id, new DateOnly(2026, 3, 1), null, CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], scoped.Select(item => item.Id));
        Assert.Equal([newer.Id], fromMarch.Select(item => item.Id));
        Assert.Null(await service.GetByIdAsync(newer.Id, otherAsset.OwnerId, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAllAsync(null, null, null, null, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(
            new ExpenseSaveModel(asset.Id, new DateOnly(2026, 3, 1), "Lamp", null, 1m, RateName, false), cancellation.Token));
    }

    [Fact]
    public async Task Handlers_create_read_update_and_delete_within_the_owner_scope()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var otherAsset = await SeedAsync(database.Context, "Other", seedRate: false);
        var service = CreateService(database.Context);
        var created = (await new ExpensesLogic.Create.CreateHandler(service).HandleAsync(
            new(Save(asset.Id, new DateOnly(2026, 3, 1), 10m)), CancellationToken.None)).Expense;
        Assert.Equal(21m, created.VatPercentage);

        var getAll = new ExpensesLogic.GetAll.GetAllHandler(service);
        Assert.Equal([created], (await getAll.HandleAsync(new(asset.OwnerId, asset.Id, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1)), CancellationToken.None)).Expenses);
        Assert.Empty((await getAll.HandleAsync(new(otherAsset.OwnerId, null, null, null), CancellationToken.None)).Expenses);
        Assert.Empty((await getAll.HandleAsync(new(null, null, null, new DateOnly(2026, 2, 28)), CancellationToken.None)).Expenses);

        var getById = new ExpensesLogic.GetById.GetByIdHandler(service);
        Assert.Equal(created, (await getById.HandleAsync(new(created.Id, asset.OwnerId), CancellationToken.None)).Expense);
        Assert.Null((await getById.HandleAsync(new(created.Id, otherAsset.OwnerId), CancellationToken.None)).Expense);

        var update = new ExpensesLogic.Update.UpdateHandler(service);
        var updated = (await update.HandleAsync(new(created.Id, Save(asset.Id, new DateOnly(2026, 3, 2), 20m)), CancellationToken.None)).Expense!;
        Assert.Equal((new DateOnly(2026, 3, 2), 20m), (updated.Date, updated.AmountExclVat));
        Assert.Null((await update.HandleAsync(new(created.Id + 100, Save(asset.Id, new DateOnly(2026, 3, 2), 20m)), CancellationToken.None)).Expense);

        var delete = new ExpensesLogic.Delete.DeleteHandler(service);
        Assert.True((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
        Assert.False((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
    }

    [Fact]
    public async Task Handlers_pass_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);
        var save = Save(asset.Id, new DateOnly(2026, 3, 1), 1m);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExpensesLogic.GetAll.GetAllHandler(service).HandleAsync(new(null, null, null, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExpensesLogic.GetById.GetByIdHandler(service).HandleAsync(new(1, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExpensesLogic.Create.CreateHandler(service).HandleAsync(new(save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExpensesLogic.Update.UpdateHandler(service).HandleAsync(new(1, save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ExpensesLogic.Delete.DeleteHandler(service).HandleAsync(new(1), cancellation.Token));
    }

    [Fact]
    public void Mappings_copy_every_field_including_the_vat_amounts()
    {
        var expense = new ExpenseModel(7, 3, "House", new DateOnly(2026, 3, 1), "Lamp", "Onderhoud", 10.01m, RateName, 21m, true, 5);
        var save = new ExpenseSaveModel(3, new DateOnly(2026, 3, 1), "Lamp", "Onderhoud", 10.01m, RateName, true);

        Assert.Equal(new ExpensesLogic.GetAll.Query(5, 3, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), ExpensesApi.GetAll.Mapping.ToQuery(
            new ExpensesApi.GetAll.Request(3, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), 5));
        Assert.Equivalent(expense, Assert.Single(ExpensesApi.GetAll.Mapping.ToResponse(new ExpensesLogic.GetAll.Result([expense]))), strict: true);
        Assert.Equal(new ExpensesLogic.GetById.Query(7, 5), ExpensesApi.GetById.Mapping.ToQuery(new ExpensesApi.GetById.Request(7), 5));
        var response = ExpensesApi.GetById.Mapping.ToResponse(new ExpensesLogic.GetById.Result(expense));
        Assert.Equivalent(expense, response, strict: true);
        Assert.Equal((2.10m, 12.11m), (response.VatAmount, response.AmountInclVat));

        Assert.Equal(new ExpensesLogic.Create.Command(save), ExpensesApi.Create.Mapping.ToCommand(
            new ExpensesApi.Create.Request(3, new DateOnly(2026, 3, 1), "Lamp", "Onderhoud", 10.01m, RateName, true)));
        Assert.Equivalent(expense, ExpensesApi.Create.Mapping.ToResponse(new ExpensesLogic.Create.Result(expense)), strict: true);

        Assert.Equal(new ExpensesLogic.Update.Command(7, save), ExpensesApi.Update.Mapping.ToCommand(
            new ExpensesApi.Update.Request(3, new DateOnly(2026, 3, 1), "Lamp", "Onderhoud", 10.01m, RateName, true), 7));
        Assert.Equivalent(expense, ExpensesApi.Update.Mapping.ToResponse(new ExpensesLogic.Update.Result(expense)), strict: true);

        Assert.Equal(new ExpensesLogic.Delete.Command(7), ExpensesApi.Delete.Mapping.ToCommand(new ExpensesApi.Delete.Request(7)));
        Assert.True(ExpensesApi.Delete.Mapping.ToResponse(new ExpensesLogic.Delete.Result(true)).Deleted);
    }

    private static ExpenseSaveModel Save(int assetId, DateOnly date, decimal amount) =>
        new(assetId, date, "Lamp", null, amount, RateName, true);

    private static ExpenseService CreateService(AppDbContext context) => new(context, new VatRateService(context));

    private static async Task<Asset> SeedAsync(AppDbContext context, string ownerName, bool seedRate = true)
    {
        var owner = new Owner { Name = ownerName };
        var asset = new Asset { Owner = owner, Name = $"{ownerName} house", Address = "Road 1", PostalCode = "1000 AA", City = "City" };
        context.Assets.Add(asset);
        if (seedRate) context.VatRates.Add(new VatRate { Name = RateName, Percentage = 21m, ValidFrom = new DateOnly(2020, 1, 1) });
        await context.SaveChangesAsync();
        return asset;
    }
}
