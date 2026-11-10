namespace PropertyAdmin.Logic.V1.Expenses.Models;

public sealed record ExpenseModel(
    int Id,
    int AssetId,
    string AssetName,
    DateOnly Date,
    string Description,
    string? Category,
    decimal AmountExclVat,
    string VatRateName,
    decimal VatPercentage,
    bool Reinvoiceable,
    int? InvoiceId)
{
    public decimal VatAmount => Money.Vat(AmountExclVat, VatPercentage);

    public decimal AmountInclVat => AmountExclVat + VatAmount;
}
