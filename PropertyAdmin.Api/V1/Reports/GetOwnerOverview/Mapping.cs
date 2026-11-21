using PropertyAdmin.Logic.V1.Reports.GetOwnerOverview;

namespace PropertyAdmin.Api.V1.Reports.GetOwnerOverview;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.From, request.To, ownerScopeId);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Owners.Select(owner => new Response(owner.OwnerId, owner.OwnerName, owner.AssetCount, owner.Income, owner.Expenses,
            owner.OpenInvoiceCount, owner.OpenInvoiceAmount,
            owner.OpenInvoices.Select(invoice => new OpenInvoiceResponse(invoice.Id, invoice.Number, invoice.Date, invoice.DueDate,
                invoice.TotalInclVat, invoice.Overdue)).ToList())).ToList();
}
