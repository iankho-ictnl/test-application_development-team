using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.V1.Invoices.GetUninvoiced;

public sealed record Response(IReadOnlyList<ActivityResponse> Activities, IReadOnlyList<ExpenseResponse> Expenses);

public sealed record ActivityResponse(
    int Id,
    int AssetId,
    string AssetName,
    DateOnly Date,
    ActivityKind Kind,
    string Description,
    PricingType PricingType,
    decimal? Hours,
    decimal? HourlyRate,
    decimal? FixedPrice,
    decimal AmountExclVat,
    string VatRateName,
    decimal VatPercentage,
    decimal VatAmount,
    decimal AmountInclVat,
    int? InvoiceId);

public sealed record ExpenseResponse(
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
