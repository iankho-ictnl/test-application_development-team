using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.V1.Activities.Update;

public sealed record Response(
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
