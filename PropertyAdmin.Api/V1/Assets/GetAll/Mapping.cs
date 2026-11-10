using PropertyAdmin.Logic.V1.Assets.GetAll;
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Api.V1.Assets.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(ownerScopeId, request.OwnerId);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Assets.Select(asset => asset.ToResponse()).ToList();

    private static Response ToResponse(this AssetModel asset) =>
        new(asset.Id, asset.OwnerId, asset.OwnerName, (AssetType)asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City);
}