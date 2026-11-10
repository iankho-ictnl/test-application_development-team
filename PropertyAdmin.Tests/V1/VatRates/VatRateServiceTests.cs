using PropertyAdmin.Data;
using PropertyAdmin.Logic;
using PropertyAdmin.Logic.V1.VatRates;
using PropertyAdmin.Logic.V1.VatRates.Models;
using PropertyAdmin.Tests.V1;
using VatRatesApi = PropertyAdmin.Api.V1.VatRates;
using VatRatesLogic = PropertyAdmin.Logic.V1.VatRates;

namespace PropertyAdmin.Tests.V1.VatRates;

public sealed class VatRateServiceTests
{
    [Fact]
    public async Task Create_rejects_overlapping_periods_and_maps_rate_response()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new VatRateService(database.Context);
        var rate = await service.CreateAsync(
            new VatRateSaveModel("Standard", 21m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            CancellationToken.None);

        await Assert.ThrowsAsync<PropertyAdmin.Logic.BusinessRuleException>(() => service.CreateAsync(
            new VatRateSaveModel("Standard", 22m, new DateOnly(2026, 6, 1), null),
            CancellationToken.None));

        var response = PropertyAdmin.Api.V1.VatRates.GetById.Mapping.ToResponse(
            new PropertyAdmin.Logic.V1.VatRates.GetById.Result(rate));
        Assert.Equal(rate.Id, response.Id);
        Assert.Equal(21m, response.Percentage);
    }

    [Theory]
    [InlineData("2019-01-01", 19)]
    [InlineData("2025-12-31", 19)]
    [InlineData("2026-01-01", 21)]
    [InlineData("2099-06-30", 21)]
    public async Task GetPercentage_resolves_the_rate_valid_on_the_date(string date, int expected)
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        await SeedRatesAsync(database.Context);

        var percentage = await new VatRateService(database.Context).GetPercentageAsync("Hoog", DateOnly.Parse(date), CancellationToken.None);

        Assert.Equal(expected, percentage);
    }

    [Theory]
    [InlineData("Hoog", "2018-12-31")]
    [InlineData("Laag", "2026-01-01")]
    [InlineData("Onbekend", "2026-01-01")]
    public async Task GetPercentage_rejects_a_date_or_name_without_a_valid_rate(string name, string date)
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        await SeedRatesAsync(database.Context);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => new VatRateService(database.Context).GetPercentageAsync(name, DateOnly.Parse(date), CancellationToken.None));

        Assert.StartsWith($"Er is geen btw-tarief '{name}' geldig op ", exception.Message);
    }

    [Fact]
    public async Task GetPercentage_takes_the_rate_of_the_requested_name()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        await SeedRatesAsync(database.Context);

        Assert.Equal(9m, await new VatRateService(database.Context).GetPercentageAsync("Laag", new DateOnly(2025, 12, 31), CancellationToken.None));
    }

    [Fact]
    public async Task Handlers_create_read_update_and_delete_a_rate()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new VatRateService(database.Context);
        var created = (await new VatRatesLogic.Create.CreateHandler(service).HandleAsync(
            new(new VatRateSaveModel(" Hoog ", 21m, new DateOnly(2026, 1, 1), null)), CancellationToken.None)).Rate;
        Assert.Equal("Hoog", created.Name);

        Assert.Equal([created], (await new VatRatesLogic.GetAll.GetAllHandler(service).HandleAsync(new(), CancellationToken.None)).Rates);
        var getById = new VatRatesLogic.GetById.GetByIdHandler(service);
        Assert.Equal(created, (await getById.HandleAsync(new(created.Id), CancellationToken.None)).Rate);
        Assert.Null((await getById.HandleAsync(new(created.Id + 100), CancellationToken.None)).Rate);

        var update = new VatRatesLogic.Update.UpdateHandler(service);
        var changed = new VatRateSaveModel("Hoog", 22m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        Assert.Equal(created with { Percentage = 22m, ValidTo = new DateOnly(2026, 12, 31) }, (await update.HandleAsync(new(created.Id, changed), CancellationToken.None)).Rate);
        Assert.Null((await update.HandleAsync(new(created.Id + 100, changed), CancellationToken.None)).Rate);
        await Assert.ThrowsAsync<BusinessRuleException>(() => update.HandleAsync(
            new(created.Id, changed with { ValidTo = new DateOnly(2025, 12, 31) }), CancellationToken.None));

        var delete = new VatRatesLogic.Delete.DeleteHandler(service);
        Assert.True((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
        Assert.False((await delete.HandleAsync(new(created.Id), CancellationToken.None)).Deleted);
    }

    [Fact]
    public async Task Handlers_and_rate_resolution_pass_a_cancellation_through()
    {
        await using var database = await MasterDataTestDatabase.CreateAsync();
        var service = new VatRateService(database.Context);
        var save = new VatRateSaveModel("Hoog", 21m, new DateOnly(2026, 1, 1), null);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VatRatesLogic.GetAll.GetAllHandler(service).HandleAsync(new(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VatRatesLogic.GetById.GetByIdHandler(service).HandleAsync(new(1), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VatRatesLogic.Create.CreateHandler(service).HandleAsync(new(save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VatRatesLogic.Update.UpdateHandler(service).HandleAsync(new(1, save), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new VatRatesLogic.Delete.DeleteHandler(service).HandleAsync(new(1), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetPercentageAsync("Hoog", new DateOnly(2026, 1, 1), cancellation.Token));
    }

    [Fact]
    public void Mappings_copy_every_field()
    {
        var rate = new VatRateModel(7, "Hoog", 21m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var save = new VatRateSaveModel("Hoog", 21m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.Equal(new VatRatesLogic.GetAll.Query(), VatRatesApi.GetAll.Mapping.ToQuery(new VatRatesApi.GetAll.Request()));
        Assert.Equivalent(rate, Assert.Single(VatRatesApi.GetAll.Mapping.ToResponse(new VatRatesLogic.GetAll.Result([rate]))), strict: true);
        Assert.Equal(new VatRatesLogic.GetById.Query(7), VatRatesApi.GetById.Mapping.ToQuery(new VatRatesApi.GetById.Request(7)));
        Assert.Equivalent(rate, VatRatesApi.GetById.Mapping.ToResponse(new VatRatesLogic.GetById.Result(rate)), strict: true);

        Assert.Equal(new VatRatesLogic.Create.Command(save), VatRatesApi.Create.Mapping.ToCommand(
            new VatRatesApi.Create.Request("Hoog", 21m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));
        Assert.Equivalent(rate, VatRatesApi.Create.Mapping.ToResponse(new VatRatesLogic.Create.Result(rate)), strict: true);

        Assert.Equal(new VatRatesLogic.Update.Command(7, save), VatRatesApi.Update.Mapping.ToCommand(
            new VatRatesApi.Update.Request("Hoog", 21m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), 7));
        Assert.Equivalent(rate, VatRatesApi.Update.Mapping.ToResponse(new VatRatesLogic.Update.Result(rate)), strict: true);

        Assert.Equal(new VatRatesLogic.Delete.Command(7), VatRatesApi.Delete.Mapping.ToCommand(new VatRatesApi.Delete.Request(7)));
        Assert.True(VatRatesApi.Delete.Mapping.ToResponse(new VatRatesLogic.Delete.Result(true)).Deleted);
    }

    private static async Task SeedRatesAsync(AppDbContext context)
    {
        context.VatRates.AddRange(
            new VatRate { Name = "Hoog", Percentage = 19m, ValidFrom = new DateOnly(2019, 1, 1), ValidTo = new DateOnly(2025, 12, 31) },
            new VatRate { Name = "Hoog", Percentage = 21m, ValidFrom = new DateOnly(2026, 1, 1) },
            new VatRate { Name = "Laag", Percentage = 9m, ValidFrom = new DateOnly(2019, 1, 1), ValidTo = new DateOnly(2025, 12, 31) });
        await context.SaveChangesAsync();
    }
}