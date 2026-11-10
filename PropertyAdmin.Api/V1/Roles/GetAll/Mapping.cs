using PropertyAdmin.Logic.V1.Roles.GetAll;

namespace PropertyAdmin.Api.V1.Roles.GetAll;

public static class Mapping
{
    public static Query ToQuery() => new();

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Roles.Select(role => new Response(role.Name, role.Permissions, role.UserCount)).ToList();
}
