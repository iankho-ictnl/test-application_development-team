using PropertyAdmin.Logic.V1.Owners.Delete;

namespace PropertyAdmin.Api.V1.Owners.Delete;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(request.Id);

    public static Response ToResponse(this Result result) => new(result.Deleted);
}