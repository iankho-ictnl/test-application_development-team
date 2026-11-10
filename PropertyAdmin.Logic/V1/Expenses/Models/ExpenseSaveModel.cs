namespace PropertyAdmin.Logic.V1.Expenses.Models;

public sealed record ExpenseSaveModel(
    int AssetId,
    DateOnly Date,
    string Description,
    string? Category,
    decimal AmountExclVat,
    string VatRateName,
    bool Reinvoiceable);
