using PropertyAdmin.Logic.V1.Owners.Models;
using PropertyAdmin.Logic.V1.Owners.Update;

namespace PropertyAdmin.Api.V1.Owners.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new OwnerSaveModel(
        request.Name,
        request.Email,
        request.Phone,
        request.Address,
        request.PostalCode,
        request.City));

    public static Response ToResponse(this Result result)
    {
        var owner = result.Owner!;
        return new Response(owner.Id, owner.Name, owner.Email, owner.Phone, owner.Address, owner.PostalCode, owner.City);
    }
}