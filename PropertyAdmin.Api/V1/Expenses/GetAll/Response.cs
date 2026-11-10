namespace PropertyAdmin.Api.V1.Expenses.GetAll;

public sealed record Response(
    int Id,
    int AssetId,
    string AssetName,
    DateOnly Date,
    string Description,
    string? Category,
    decimal AmountExclVat,
    string VatRateName,
    decimal VatPercentage,
    decimal VatAmount,
    decimal AmountInclVat,
    bool Reinvoiceable,
    int? InvoiceId);
