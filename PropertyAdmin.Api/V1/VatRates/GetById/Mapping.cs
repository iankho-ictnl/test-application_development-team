using PropertyAdmin.Logic.V1.VatRates.GetById;
using PropertyAdmin.Logic.V1.VatRates.Models;

namespace PropertyAdmin.Api.V1.VatRates.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request) => new(request.Id);

    public static Response ToResponse(this Result result) => result.Rate!.ToResponse();

    private static Response ToResponse(this VatRateModel rate) =>
        new(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo);
}