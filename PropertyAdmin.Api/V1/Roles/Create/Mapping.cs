using PropertyAdmin.Logic.V1.Roles.Create;
using PropertyAdmin.Logic.V1.Roles.Models;

namespace PropertyAdmin.Api.V1.Roles.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(new RoleSaveModel(request.Name, request.Permissions));
}
