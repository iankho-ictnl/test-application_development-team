using PropertyAdmin.Logic.V1.Invoices.Create;
using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Api.V1.Invoices.Create;

public static class Mapping
{
    public static Command ToCommand(this Request request) => new(new InvoiceCreateModel(request.OwnerId, request.Date, request.PaymentTermDays));

    public static Response ToResponse(this Result result)
    {
        var invoice = result.Invoice;
        return new Response(invoice.Id, invoice.Number, invoice.OwnerId, invoice.OwnerName, invoice.Date, invoice.DueDate, (InvoiceStatus)invoice.Status,
            invoice.PaidDate, invoice.TotalExclVat, invoice.TotalVat, invoice.TotalInclVat,
            invoice.Lines.Select(line => new LineResponse(line.Description, line.AmountExclVat, line.VatPercentage, line.VatAmount, line.AmountInclVat)).ToList(),
            invoice.VatSummary.Select(summary => new VatSummaryResponse(summary.VatPercentage, summary.Base, summary.VatAmount)).ToList());
    }
}
