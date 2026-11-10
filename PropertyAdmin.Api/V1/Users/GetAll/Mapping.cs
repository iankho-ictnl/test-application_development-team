using PropertyAdmin.Logic.V1.Users.GetAll;

namespace PropertyAdmin.Api.V1.Users.GetAll;

public static class Mapping
{
    public static Query ToQuery() => new();

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Users.Select(user => new Response(user.Id, user.UserName, user.Email, user.TwoFactorEnabled, user.LockedOut, user.OwnerId, user.Roles)).ToList();
}
