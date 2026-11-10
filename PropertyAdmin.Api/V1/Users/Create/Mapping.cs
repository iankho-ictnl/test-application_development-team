using PropertyAdmin.Logic.V1.Users.Create;
using PropertyAdmin.Logic.V1.Users.Models;

namespace PropertyAdmin.Api.V1.Users.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) =>
        new(new UserCreateModel(request.UserName, request.Email, request.Password, request.OwnerId, request.Roles));

    public static Response ToResponse(this Result result)
    {
        var user = result.User;
        return new Response(user.Id, user.UserName, user.Email, user.TwoFactorEnabled, user.LockedOut, user.OwnerId, user.Roles);
    }
}
