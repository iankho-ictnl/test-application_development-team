using PropertyAdmin.Logic.V1.Activities.GetById;

namespace PropertyAdmin.Api.V1.Activities.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.Id, ownerScopeId);

    public static Response ToResponse(this Result result)
    {
        var activity = result.Activity!;
        return new Response(activity.Id, activity.AssetId, activity.AssetName, activity.Date, (ActivityKind)activity.Kind, activity.Description, (PricingType)activity.PricingType,
            activity.Hours, activity.HourlyRate, activity.FixedPrice, activity.AmountExclVat, activity.VatRateName, activity.VatPercentage,
            activity.VatAmount, activity.AmountInclVat, activity.InvoiceId);
    }
}
