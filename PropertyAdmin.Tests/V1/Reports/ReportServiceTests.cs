using PropertyAdmin.Contracts;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Reports;

namespace PropertyAdmin.Tests.V1.Reports;

public sealed class ReportServiceTests
{
    private static readonly DateOnly From = new(2025, 1, 1);
    private static readonly DateOnly To = new(2025, 12, 31);

    [Fact]
    public async Task GetAssetResults_sums_income_and_expenses_in_the_period_per_asset()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var (asset, otherAsset) = await SeedAsync(database.Context);
        var service = new ReportService(database.Context);

        var results = await service.GetAssetResultsAsync(From, To, null, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Reports.GetAssetResults.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Reports.GetAssetResults.Result(results));

        Assert.Equal([asset.Id, otherAsset.Id], response.Select(row => row.AssetId));
        var row = response[0];
        Assert.Equal("Alpha", row.OwnerName);
        Assert.Equal(200.10m, row.Income);
        Assert.Equal(100m, row.Expenses);
        Assert.Equal(100.10m, row.Result);
        Assert.Equal(0m, response[1].Income);
        Assert.Equal(-40m, response[1].Result);
    }

    [Fact]
    public async Task GetOwnerOverview_counts_assets_and_open_invoices_with_the_invoice_rounding()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var (asset, otherAsset) = await SeedAsync(database.Context);
        var service = new ReportService(database.Context);

        var overview = await service.GetOwnerOverviewAsync(From, To, null, CancellationToken.None);
        var response = PropertyAdmin.Api.V1.Reports.GetOwnerOverview.Mapping.ToResponse(new PropertyAdmin.Logic.V1.Reports.GetOwnerOverview.Result(overview));

        Assert.Equal(["Alpha", "Beta"], response.Select(owner => owner.OwnerName));
        var alpha = response[0];
        Assert.Equal(asset.OwnerId, alpha.OwnerId);
        Assert.Equal(1, alpha.AssetCount);
        Assert.Equal(200.10m, alpha.Income);
        Assert.Equal(100m, alpha.Expenses);
        Assert.Equal(1, alpha.OpenInvoiceCount);
        // VAT once over the 21% base of 100.06 is 21.01 (rounded per line it would be 21.00).
        Assert.Equal(100.06m + 21.01m, alpha.OpenInvoiceAmount);
        Assert.Equal(otherAsset.OwnerId, response[1].OwnerId);
        Assert.Equal(0, response[1].OpenInvoiceCount);
    }

    [Fact]
    public async Task Owner_scope_limits_both_reports_to_that_owner()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var (_, otherAsset) = await SeedAsync(database.Context);
        var service = new ReportService(database.Context);

        var results = await service.GetAssetResultsAsync(From, To, otherAsset.OwnerId, CancellationToken.None);
        var overview = await service.GetOwnerOverviewAsync(From, To, otherAsset.OwnerId, CancellationToken.None);

        Assert.Equal([otherAsset.Id], results.Select(row => row.AssetId));
        Assert.Equal([otherAsset.OwnerId], overview.Select(owner => owner.OwnerId));
        Assert.Equal(0, overview[0].OpenInvoiceCount);
    }

    [Fact]
    public async Task Cancelled_request_passes_the_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var handler = new PropertyAdmin.Logic.V1.Reports.GetOwnerOverview.GetOwnerOverviewHandler(new ReportService(database.Context));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new PropertyAdmin.Logic.V1.Reports.GetOwnerOverview.Query(From, To, null), cancellation.Token));
    }

    private static async Task<(Asset Asset, Asset OtherAsset)> SeedAsync(AppDbContext context)
    {
        var asset = new Asset { Owner = new Owner { Name = "Alpha" }, Name = "Alpha house", Address = "Road 1", PostalCode = "1000 AA", City = "City" };
        var otherAsset = new Asset { Owner = new Owner { Name = "Beta" }, Name = "Beta house", Address = "Road 2", PostalCode = "1000 AA", City = "City" };
        context.Assets.AddRange(asset, otherAsset);
        await context.SaveChangesAsync();

        context.Activities.AddRange(
            Activity(asset, new DateOnly(2025, 6, 1), 200m),
            Activity(asset, new DateOnly(2025, 12, 31), 0.10m),
            Activity(asset, new DateOnly(2024, 12, 31), 999m));
        context.Expenses.AddRange(
            Expense(asset, new DateOnly(2025, 6, 2), 100m),
            Expense(asset, new DateOnly(2026, 1, 1), 999m),
            Expense(otherAsset, new DateOnly(2025, 3, 1), 40m));
        var open = new Invoice { Number = "2025-0001", OwnerId = asset.OwnerId, Date = new DateOnly(2025, 6, 30), DueDate = new DateOnly(2025, 7, 30), Status = InvoiceStatus.Open };
        open.Lines.AddRange([Line(0.02m), Line(0.02m), Line(0.02m), Line(100m)]);
        var paid = new Invoice { Number = "2025-0002", OwnerId = otherAsset.OwnerId, Date = new DateOnly(2025, 6, 30), DueDate = new DateOnly(2025, 7, 30), Status = InvoiceStatus.Paid };
        paid.Lines.Add(Line(50m));
        context.Invoices.AddRange(open, paid);
        await context.SaveChangesAsync();
        return (asset, otherAsset);
    }

    private static Activity Activity(Asset asset, DateOnly date, decimal amount) =>
        new() { AssetId = asset.Id, Date = date, Description = "Activity", PricingType = PricingType.Fixed, FixedPrice = amount, AmountExclVat = amount, VatRateName = "Hoog", VatPercentage = 21m };

    private static Expense Expense(Asset asset, DateOnly date, decimal amount) =>
        new() { AssetId = asset.Id, Date = date, Description = "Expense", AmountExclVat = amount, VatRateName = "Hoog", VatPercentage = 21m };

    private static InvoiceLine Line(decimal amount) =>
        new() { Description = "Line", AmountExclVat = amount, VatPercentage = 21m, SourceType = InvoiceLineSource.Activity, SourceId = 0 };
}
