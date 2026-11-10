using PropertyAdmin.Logic.V1.Assets.GetById;
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Api.V1.Assets.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.Id, ownerScopeId);

    public static Response ToResponse(this Result result) => result.Asset!.ToResponse();

    private static Response ToResponse(this AssetModel asset) =>
        new(asset.Id, asset.OwnerId, asset.OwnerName, (AssetType)asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City);
}