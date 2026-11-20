using PropertyAdmin.Logic.V1.Owners.Models;
using PropertyAdmin.Logic.V1.Owners.Update;

namespace PropertyAdmin.Api.V1.Owners.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, new OwnerSaveModel(
        request.Name,
        request.Email,
        request.Phone,
        request.Street,
        request.HouseNumber,
        request.PostalCode,
        request.City,
        request.Country,
        request.KvkNumber,
        request.VatNumber));

    public static Response ToResponse(this Result result)
    {
        var owner = result.Owner!;
        return new Response(owner.Id, owner.Name, owner.Email, owner.Phone, owner.Street, owner.HouseNumber,
            owner.PostalCode, owner.City, owner.Country, owner.KvkNumber, owner.VatNumber);
    }
}