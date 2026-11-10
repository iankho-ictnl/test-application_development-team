using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.VatRates.Update;

public sealed record Request([Required, MaxLength(50)] string Name, [Range(0, 100)] decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);