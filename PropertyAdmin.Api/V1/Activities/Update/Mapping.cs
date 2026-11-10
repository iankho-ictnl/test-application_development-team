using PropertyAdmin.Logic.V1.Activities.Models;
using PropertyAdmin.Logic.V1.Activities.Update;

namespace PropertyAdmin.Api.V1.Activities.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new ActivitySaveModel(
        request.AssetId, request.Date, (PropertyAdmin.Data.ActivityKind)request.Kind, request.Description,
        (PropertyAdmin.Data.PricingType)request.PricingType, request.Hours, request.HourlyRate,
        request.FixedPrice, request.VatRateName));

    public static Response ToResponse(this Result result)
    {
        var activity = result.Activity!;
        return new Response(activity.Id, activity.AssetId, activity.AssetName, activity.Date, (ActivityKind)activity.Kind, activity.Description, (PricingType)activity.PricingType,
            activity.Hours, activity.HourlyRate, activity.FixedPrice, activity.AmountExclVat, activity.VatRateName, activity.VatPercentage,
            activity.VatAmount, activity.AmountInclVat, activity.InvoiceId);
    }
}
