using System.ComponentModel.DataAnnotations;
using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.V1.Activities.Create;

public sealed record Request(
    [Range(1, int.MaxValue)] int AssetId,
    DateOnly Date,
    ActivityKind Kind,
    [Required, MaxLength(500)] string Description,
    PricingType PricingType,
    [Range(0, 100000)] decimal? Hours,
    [Range(0, 1000000)] decimal? HourlyRate,
    [Range(0, 999999999)] decimal? FixedPrice,
    [Required] string VatRateName);
