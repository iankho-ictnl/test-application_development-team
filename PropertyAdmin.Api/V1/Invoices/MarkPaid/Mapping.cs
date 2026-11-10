using PropertyAdmin.Logic.V1.Invoices.MarkPaid;

namespace PropertyAdmin.Api.V1.Invoices.MarkPaid;

public static class Mapping
{
    public static Command ToCommand(this Request request, int id) => new(id, request.PaidDate);
}
