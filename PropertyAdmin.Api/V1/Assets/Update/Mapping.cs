using PropertyAdmin.Logic.V1.Assets.Models;
using PropertyAdmin.Logic.V1.Assets.Update;

namespace PropertyAdmin.Api.V1.Assets.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new AssetSaveModel(
        request.OwnerId, (PropertyAdmin.Data.AssetType)request.Type, request.Name, request.Street, request.HouseNumber, request.PostalCode, request.City, request.Country));

    public static Response ToResponse(this Result result)
    {
        var asset = result.Asset!;
        return new Response(asset.Id, asset.OwnerId, asset.OwnerName, (AssetType)asset.Type, asset.Name, asset.Street, asset.HouseNumber, asset.PostalCode, asset.City, asset.Country);
    }
}