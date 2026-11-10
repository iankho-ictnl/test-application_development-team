using PropertyAdmin.Logic.V1.Invoices.GetAll;
using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Api.V1.Invoices.GetAll;

public static class Mapping
{
    public static Query ToQuery(this Request request, int? ownerScopeId) => new(ownerScopeId, request.OwnerId, (PropertyAdmin.Data.InvoiceStatus?)request.Status);

    public static IReadOnlyList<Response> ToResponse(this Result result) =>
        result.Invoices.Select(invoice => invoice.ToResponse()).ToList();

    private static Response ToResponse(this InvoiceModel invoice) =>
        new(invoice.Id, invoice.Number, invoice.OwnerId, invoice.OwnerName, invoice.Date, invoice.DueDate, (InvoiceStatus)invoice.Status, invoice.PaidDate,
            invoice.TotalExclVat, invoice.TotalVat, invoice.TotalInclVat,
            invoice.Lines.Select(line => new LineResponse(line.Description, line.AmountExclVat, line.VatPercentage, line.VatAmount, line.AmountInclVat)).ToList(),
            invoice.VatSummary.Select(summary => new VatSummaryResponse(summary.VatPercentage, summary.Base, summary.VatAmount)).ToList());
}
