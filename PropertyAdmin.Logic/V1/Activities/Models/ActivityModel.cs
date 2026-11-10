using PropertyAdmin.Data;

namespace PropertyAdmin.Logic.V1.Activities.Models;

public sealed record ActivityModel(
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
    int? InvoiceId)
{
    public decimal VatAmount => Money.Vat(AmountExclVat, VatPercentage);

    public decimal AmountInclVat => AmountExclVat + VatAmount;
}
