using PropertyAdmin.Logic.V1.VatRates.Models;
using PropertyAdmin.Logic.V1.VatRates.Update;

namespace PropertyAdmin.Api.V1.VatRates.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new VatRateSaveModel(request.Name, request.Percentage, request.ValidFrom, request.ValidTo));

    public static Response ToResponse(this Result result)
    {
        var rate = result.Rate!;
        return new Response(rate.Id, rate.Name, rate.Percentage, rate.ValidFrom, rate.ValidTo);
    }
}