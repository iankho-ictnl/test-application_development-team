using PropertyAdmin.Logic.V1.VatRates;
using PropertyAdmin.Logic.V1.VatRates.Models;
using PropertyAdmin.Tests.V1;

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
}