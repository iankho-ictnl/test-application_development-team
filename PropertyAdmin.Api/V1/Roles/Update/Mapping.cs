using PropertyAdmin.Logic.V1.Roles.Models;
using PropertyAdmin.Logic.V1.Roles.Update;

namespace PropertyAdmin.Api.V1.Roles.Update;

public static class Mapping
{
    public static Command ToCommand(this Request request, string name) => new(name, new RoleSaveModel(request.Name, request.Permissions));
}
