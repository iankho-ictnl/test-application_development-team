using PropertyAdmin.Logic.V1.Activities.GetAll;
using PropertyAdmin.Logic.V1.Activities.Models;

namespace PropertyAdmin.Api.V1.Activities.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(ownerScopeId, request.AssetId, request.From, request.To);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Activities.Select(activity => activity.ToResponse()).ToList();

    private static Response ToResponse(this ActivityModel activity) =>
        new(activity.Id, activity.AssetId, activity.AssetName, activity.Date, (ActivityKind)activity.Kind, activity.Description, (PricingType)activity.PricingType,
            activity.Hours, activity.HourlyRate, activity.FixedPrice, activity.AmountExclVat, activity.VatRateName, activity.VatPercentage,
            activity.VatAmount, activity.AmountInclVat, activity.InvoiceId);
}
