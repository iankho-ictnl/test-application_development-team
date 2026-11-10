using PropertyAdmin.Logic.V1.Users.GetById;

namespace PropertyAdmin.Api.V1.Users.GetById;

public static class Mapping
{
    public static Query ToQuery(string id) => new(id);

    public static Response ToResponse(this Result result)
    {
        var user = result.User!;
        return new Response(user.Id, user.UserName, user.Email, user.TwoFactorEnabled, user.LockedOut, user.OwnerId, user.Roles);
    }
}
