using PropertyAdmin.Data;

namespace PropertyAdmin.Logic.V1.Invoices.Models;

public sealed record InvoiceModel(
    int Id,
    string Number,
    int OwnerId,
    string OwnerName,
    DateOnly Date,
    DateOnly DueDate,
    InvoiceStatus Status,
    DateOnly? PaidDate,
    IReadOnlyList<InvoiceLineModel> Lines)
{
    public IReadOnlyList<VatSummaryLine> VatSummary { get; } =
        InvoiceTotals.VatSummary(Lines.Select(line => (line.AmountExclVat, line.VatPercentage)));

    public decimal TotalExclVat => VatSummary.Sum(summary => summary.Base);

    public decimal TotalVat => VatSummary.Sum(summary => summary.VatAmount);

    public decimal TotalInclVat => TotalExclVat + TotalVat;
}
