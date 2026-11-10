using PropertyAdmin.Logic.V1.Invoices.Delete;

namespace PropertyAdmin.Api.V1.Invoices.Delete;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(request.Id);
}
