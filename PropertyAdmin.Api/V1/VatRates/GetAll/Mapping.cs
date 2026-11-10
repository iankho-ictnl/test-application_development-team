using PropertyAdmin.Logic.V1.VatRates.GetAll;
using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Api.V1.VatRates.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request) => new();

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Rates.Select(rate => rate.ToResponse()).ToList();

    private static Response ToResponse(this VatRateModel rate) =>
        new(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo);
}