using PropertyAdmin.Logic.V1.Invoices.GetPdf;

namespace PropertyAdmin.Api.V1.Invoices.GetPdf;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.Id, ownerScopeId);
}
