using PropertyAdmin.Logic.V1.Users.Models;
using PropertyAdmin.Logic.V1.Users.Update;

namespace PropertyAdmin.Api.V1.Users.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, string id) =>
        new(id, new UserUpdateModel(request.Email, request.OwnerId, request.Roles, request.LockedOut));

    public static Response ToResponse(this Result result)
    {
        var user = result.User!;
        return new Response(user.Id, user.UserName, user.Email, user.TwoFactorEnabled, user.LockedOut, user.OwnerId, user.Roles);
    }
}
