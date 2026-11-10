using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.Activities;
using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.VatRates;

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
