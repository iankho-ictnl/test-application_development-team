using PropertyAdmin.Logic.V1.Owners.GetAll;
using PropertyAdmin.Logic.V1.Owners.Models;

namespace PropertyAdmin.Api.V1.Owners.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerId) => new(ownerId ?? request.OwnerId);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Owners.Select(owner => owner.ToResponse()).ToList();

    private static Response ToResponse(this OwnerModel owner) => new(
        owner.Id,
        owner.Name,
        owner.Email,
        owner.Phone,
        owner.Address,
        owner.PostalCode,
        owner.City);
}