using PropertyAdmin.Logic.V1.Assets.Create;
using PropertyAdmin.Logic.V1.Assets.Models;

namespace PropertyAdmin.Api.V1.Assets.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(new AssetSaveModel(
        request.OwnerId, request.Type, request.Name, request.Address, request.PostalCode, request.City));

    public static Response ToResponse(this Result result)
    {
        var asset = result.Asset;
        return new Response(asset.Id, asset.OwnerId, asset.OwnerName, asset.Type, asset.Name, asset.Address, asset.PostalCode, asset.City);
    }
}