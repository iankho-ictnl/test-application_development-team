using PropertyAdmin.Logic.V1.Owners.GetById;
using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Api.V1.Owners.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerId) => new(request.Id, ownerId);

    public static Response ToResponse(this Result result) => result.Owner!.ToResponse();

    private static Response ToResponse(this OwnerModel owner) => new(
        owner.Id,
        owner.Name,
        owner.Email,
        owner.Phone,
        owner.Address,
        owner.PostalCode,
        owner.City);
}