using PropertyAdmin.Logic.V1.Invoices.GetById;

namespace PropertyAdmin.Api.V1.Invoices.GetById;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(request.Id, ownerScopeId);

    public static Response ToResponse(this Result result)
    {
        var invoice = result.Invoice!;
        return new Response(invoice.Id, invoice.Number, invoice.OwnerId, invoice.OwnerName, invoice.Date, invoice.DueDate, invoice.Status,
            invoice.PaidDate, invoice.TotalExclVat, invoice.TotalVat, invoice.TotalInclVat,
            invoice.Lines.Select(line => new LineResponse(line.Description, line.AmountExclVat, line.VatPercentage, line.VatAmount, line.AmountInclVat)).ToList(),
            invoice.VatSummary.Select(summary => new VatSummaryResponse(summary.VatPercentage, summary.Base, summary.VatAmount)).ToList());
    }
}
