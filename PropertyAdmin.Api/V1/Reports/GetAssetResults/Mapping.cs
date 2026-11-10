using PropertyAdmin.Logic.V1.Reports.GetAssetResults;

namespace PropertyAdmin.Api.V1.Reports.GetAssetResults;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.From, request.To, ownerScopeId);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.AssetResults.Select(asset => new Response(asset.AssetId, asset.AssetName, asset.OwnerName, asset.Income, asset.Expenses, asset.Result)).ToList();
}
