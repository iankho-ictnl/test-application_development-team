using PropertyAdmin.Contracts;

namespace PropertyAdmin.Logic.V1.Activities.Models;

public sealed record ActivitySaveModel(
    int AssetId,
    DateOnly Date,
    ActivityKind Kind,
    string Description,
    PricingType PricingType,
    decimal? Hours,
    decimal? HourlyRate,
    decimal? FixedPrice,
    string VatRateName);
