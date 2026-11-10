using PropertyAdmin.Logic.V1.Activities.Create;
using PropertyAdmin.Logic.V1.Activities.Models;

namespace PropertyAdmin.Api.V1.Activities.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(new ActivitySaveModel(
        request.AssetId, request.Date, request.Kind, request.Description, request.PricingType, request.Hours, request.HourlyRate,
        request.FixedPrice, request.VatRateName));

    public static Response ToResponse(this Result result)
    {
        var activity = result.Activity;
        return new Response(activity.Id, activity.AssetId, activity.AssetName, activity.Date, activity.Kind, activity.Description, activity.PricingType,
            activity.Hours, activity.HourlyRate, activity.FixedPrice, activity.AmountExclVat, activity.VatRateName, activity.VatPercentage,
            activity.VatAmount, activity.AmountInclVat, activity.InvoiceId);
    }
}
