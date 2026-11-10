namespace PropertyAdmin.Logic.V1.Invoices.Models;

public sealed record InvoiceLineModel(string Description, decimal AmountExclVat, decimal VatPercentage)
{
    public decimal VatAmount => Money.Vat(AmountExclVat, VatPercentage);

    public decimal AmountInclVat => AmountExclVat + VatAmount;
}
