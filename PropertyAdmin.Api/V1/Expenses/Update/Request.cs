using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Expenses.Update;

public sealed record Request(
    [Range(1, int.MaxValue)] int AssetId,
    DateOnly Date,
    [Required, MaxLength(500)] string Description,
    [MaxLength(100)] string? Category,
    [Range(0, 999999999)] decimal AmountExclVat,
    [Required] string VatRateName,
    bool Reinvoiceable);
