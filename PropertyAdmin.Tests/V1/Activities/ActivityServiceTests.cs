using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Activities;
using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.VatRates;
using ActivitiesApi = PropertyAdmin.Api.V1.Activities;
using ActivitiesLogic = PropertyAdmin.Logic.V1.Activities;

namespace PropertyAdmin.Tests.V1.Activities;

public sealed class ActivityServiceTests
{
    private const string RateName = "TestHoog";

    [Fact]
    public async Task Create_hourly_calculates_amount_clears_fixed_price_and_maps_response()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);

        var activity = await service.CreateAsync(
            new ActivitySaveModel(asset.Id, new DateOnly(2026, 3, 1), ActivityKind.Administration, "  Administratie  ", PricingType.Hourly, 2.5m, 40.01m, 99m, RateName),
            CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Activities.Create.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Activities.Create.Result(activity));

        Assert.Equal("Administratie", response.Description);
        Assert.Equal(asset.Name, response.AssetName);
        Assert.Equal(100.03m, response.AmountExclVat);
        Assert.Equal(2.5m, response.Hours);
        Assert.Equal(40.01m, response.HourlyRate);
        Assert.Null(response.FixedPrice);
        Assert.Equal(21m, response.VatPercentage);
        Assert.Equal(21.01m, response.VatAmount);
        Assert.Equal(121.04m, response.AmountInclVat);
        Assert.Null(response.InvoiceId);
    }

    [Fact]
    public async Task Create_fixed_clears_hours_and_rate()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);

        var activity = await service.CreateAsync(
            new ActivitySaveModel(asset.Id, new DateOnly(2026, 3, 1), ActivityKind.Maintenance, "Inspectie", PricingType.Fixed, 3m, 50m, 200.005m, RateName),
            CancellationToken.None);

        Assert.Equal(200.01m, activity.AmountExclVat);
        Assert.Equal(200.005m, activity.FixedPrice);
        Assert.Null(activity.Hours);
        Assert.Null(activity.HourlyRate);
    }

    [Theory]
    [InlineData(PricingType.Hourly, null, 40.0, null, "Vul uren en uurtarief in.")]
    [InlineData(PricingType.Hourly, 0.0, 40.0, null, "Vul uren en uurtarief in.")]
    [InlineData(PricingType.Fixed, 2.0, 40.0, null, "Vul een vaste prijs in.")]
    public void CalculateAmount_rejects_missing_pricing_values(PricingType type, double? hours, double? rate, double? fixedPrice, string message)
    {
        var exception = Assert.Throws<BusinessRuleException>(() => ActivityService.CalculateAmount(
            type, (decimal?)hours, (decimal?)rate, (decimal?)fixedPrice));

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task Create_rejects_unknown_asset_and_vat_rate_without_a_valid_period()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);

        var unknownAsset = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            Fixed(asset.Id + 100, new DateOnly(2026, 3, 1)), CancellationToken.None));
        var noRate = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            Fixed(asset.Id, new DateOnly(2019, 1, 1)), CancellationToken.None));

        Assert.Equal("Object bestaat niet.", unknownAsset.Message);
        Assert.StartsWith($"Er is geen btw-tarief '{RateName}'", noRate.Message);
    }

    [Fact]
    public async Task Update_and_delete_reject_an_invoiced_activity()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var invoice = new Invoice { Number = "2026-0001", OwnerId = asset.OwnerId, Date = new DateOnly(2026, 4, 1), DueDate = new DateOnly(2026, 5, 1) };
        database.Context.Invoices.Add(invoice);
        await database.Context.SaveChangesAsync();
        var activity = new Activity { AssetId = asset.Id, Date = new DateOnly(2026, 3, 1), Description = "Inspectie", PricingType = PricingType.Fixed, FixedPrice = 1m, VatRateName = RateName, InvoiceId = invoice.Id };
        database.Context.Activities.Add(activity);
        await database.Context.SaveChangesAsync();
        var service = CreateService(database.Context);

        var update = await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateAsync(activity.Id, Fixed(asset.Id, new DateOnly(2026, 3, 1)), CancellationToken.None));
        var delete = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(activity.Id, CancellationToken.None));
        Assert.Equal("Deze activiteit is gefactureerd en kan niet worden gewijzigd.", update.Message);
        Assert.Equal("Deze activiteit is gefactureerd en kan niet worden verwijderd.", delete.Message);
        Assert.Null(await service.UpdateAsync(activity.Id + 100, Fixed(asset.Id, new DateOnly(2026, 3, 1)), CancellationToken.None));
        Assert.False(await service.DeleteAsync(activity.Id + 100, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_applies_owner_scope_and_filters_newest_first()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var otherAsset = await SeedAsync(database.Context, "Other", seedRate: false);
        var service = CreateService(database.Context);
        var older = await service.CreateAsync(Fixed(asset.Id, new DateOnly(2026, 2, 1)), CancellationToken.None);
        var newer = await service.CreateAsync(Fixed(asset.Id, new DateOnly(2026, 3, 1)), CancellationToken.None);
        await service.CreateAsync(Fixed(otherAsset.Id, new DateOnly(2026, 3, 1)), CancellationToken.None);

        var scoped = await service.GetAllAsync(asset.OwnerId, null, null, null, CancellationToken.None);
        var fromMarch = await service.GetAllAsync(null, asset.Id, new DateOnly(2026, 3, 1), null, CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], scoped.Select(item => item.Id));
        Assert.Equal([newer.Id], fromMarch.Select(item => item.Id));
        Assert.Null(await service.GetByIdAsync(newer.Id, otherAsset.OwnerId, CancellationToken.None));
        Assert.Equal(asset.Name, (await service.GetByIdAsync(newer.Id, asset.OwnerId, CancellationToken.None))!.AssetName);
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
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(Fixed(asset.Id, new DateOnly(2026, 3, 1)), cancellation.Token));
    }

    [Fact]
    public async Task Handlers_create_read_update_and_delete_within_the_owner_scope()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var otherAsset = await SeedAsync(database.Context, "Other", seedRate: false);
        var service = CreateService(database.Context);
        var created = (await new ActivitiesLogic.Create.CreateHandler(service).HandleAsync(
            new(Fixed(asset.Id, new DateOnly(2026, 3, 1))), CancellationToken.None)).Activity;
        Assert.Equal((10m, 21m), (created.AmountExclVat, created.VatPercentage));

        var getAll = new ActivitiesLogic.GetAll.GetAllHandler(service);
        Assert.Equal([created], (await getAll.HandleAsync(new(asset.OwnerId, asset.Id, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1)), CancellationToken.None)).Activities);
        Assert.Empty((await getAll.HandleAsync(new(otherAsset.OwnerId, null, null, null), CancellationToken.None)).Activities);
        Assert.Empty((await getAll.HandleAsync(new(null, null, null, new DateOnly(2026, 2, 28)), CancellationToken.None)).Activities);

        var getById = new ActivitiesLogic.GetById.GetByIdHandler(service);
        Assert.Equal(created, (await getById.HandleAsync(new(created.Id, asset.OwnerId), CancellationToken.None)).Activity);
        Assert.Null((await getById.HandleAsync(new(created.Id, otherAsset.OwnerId), CancellationToken.None)).Activity);

        var update = new ActivitiesLogic.Update.UpdateHandler(service);
        var hourly = new ActivitySaveModel(asset.Id, new DateOnly(2026, 3, 2), ActivityKind.Administration, "Uren", PricingType.Hourly, 2m, 50m, null, RateName);
        var updated = (await update.HandleAsync(new(created.Id, hourly), CancellationToken.None)).Activity!;
        Assert.Equal((PricingType.Hourly, 100m, (decimal?)null), (updated.PricingType, updated.AmountExclVat, updated.FixedPrice));
        Assert.Null((await update.HandleAsync(new(created.Id + 100, hourly), CancellationToken.None)).Activity);

        var delete = new ActivitiesLogic.Delete.DeleteHandler(service);
        Assert.True((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
        Assert.False((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
    }

    [Fact]
    public async Task Handlers_pass_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var asset = await SeedAsync(database.Context, "Owner");
        var service = CreateService(database.Context);
        var save = Fixed(asset.Id, new DateOnly(2026, 3, 1));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActivitiesLogic.GetAll.GetAllHandler(service).HandleAsync(new(null, null, null, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActivitiesLogic.GetById.GetByIdHandler(service).HandleAsync(new(1, null), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActivitiesLogic.Create.CreateHandler(service).HandleAsync(new(save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActivitiesLogic.Update.UpdateHandler(service).HandleAsync(new(1, save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActivitiesLogic.Delete.DeleteHandler(service).HandleAsync(new(1), cancellation.Token));
    }

    [Fact]
    public void Mappings_copy_every_field_and_convert_the_enums()
    {
        var activity = new ActivityModel(7, 3, "House", new DateOnly(2026, 3, 1), ActivityKind.Maintenance, "Inspectie", PricingType.Fixed,
            null, null, 100m, 100m, RateName, 21m, 5);
        var save = new ActivitySaveModel(3, new DateOnly(2026, 3, 1), ActivityKind.Maintenance, "Inspectie", PricingType.Fixed, null, null, 100m, RateName);
        var expected = new
        {
            Id = 7, AssetId = 3, AssetName = "House", Date = new DateOnly(2026, 3, 1), Kind = PropertyAdmin.Api.V1.ActivityKind.Maintenance,
            Description = "Inspectie", PricingType = PropertyAdmin.Api.V1.PricingType.Fixed, Hours = (decimal?)null, HourlyRate = (decimal?)null,
            FixedPrice = (decimal?)100m, AmountExclVat = 100m, VatRateName = RateName, VatPercentage = 21m, VatAmount = 21m, AmountInclVat = 121m,
            InvoiceId = (int?)5,
        };

        Assert.Equal(new ActivitiesLogic.GetAll.Query(5, 3, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), ActivitiesApi.GetAll.Mapping.ToQuery(
            new ActivitiesApi.GetAll.Request(3, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), 5));
        Assert.Equivalent(expected, Assert.Single(ActivitiesApi.GetAll.Mapping.ToResponse(new ActivitiesLogic.GetAll.Result([activity]))), strict: true);
        Assert.Equal(new ActivitiesLogic.GetById.Query(7, 5), ActivitiesApi.GetById.Mapping.ToQuery(new ActivitiesApi.GetById.Request(7), 5));
        Assert.Equivalent(expected, ActivitiesApi.GetById.Mapping.ToResponse(new ActivitiesLogic.GetById.Result(activity)), strict: true);

        Assert.Equal(new ActivitiesLogic.Create.Command(save), ActivitiesApi.Create.Mapping.ToCommand(new ActivitiesApi.Create.Request(
            3, new DateOnly(2026, 3, 1), PropertyAdmin.Api.V1.ActivityKind.Maintenance, "Inspectie", PropertyAdmin.Api.V1.PricingType.Fixed, null, null, 100m, RateName)));
        Assert.Equivalent(expected, ActivitiesApi.Create.Mapping.ToResponse(new ActivitiesLogic.Create.Result(activity)), strict: true);

        Assert.Equal(new ActivitiesLogic.Update.Command(7, save), ActivitiesApi.Update.Mapping.ToCommand(new ActivitiesApi.Update.Request(
            3, new DateOnly(2026, 3, 1), PropertyAdmin.Api.V1.ActivityKind.Maintenance, "Inspectie", PropertyAdmin.Api.V1.PricingType.Fixed, null, null, 100m, RateName), 7));
        Assert.Equivalent(expected, ActivitiesApi.Update.Mapping.ToResponse(new ActivitiesLogic.Update.Result(activity)), strict: true);

        Assert.Equal(new ActivitiesLogic.Delete.Command(7), ActivitiesApi.Delete.Mapping.ToCommand(new ActivitiesApi.Delete.Request(7)));
        Assert.True(ActivitiesApi.Delete.Mapping.ToResponse(new ActivitiesLogic.Delete.Result(true)).Deleted);
    }

    private static ActivitySaveModel Fixed(int assetId, DateOnly date) =>
        new(assetId, date, ActivityKind.Maintenance, "Inspectie", PricingType.Fixed, null, null, 10m, RateName);

    private static ActivityService CreateService(AppDbContext context) => new(context, new VatRateService(context));

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
