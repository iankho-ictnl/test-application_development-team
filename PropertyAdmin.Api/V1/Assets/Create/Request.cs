using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Assets.Create;

public sealed record Request(
    [Range(1, int.MaxValue)] int OwnerId,
    AssetType Type,
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(200)] string Address,
    [Required, MaxLength(20)] string PostalCode,
    [Required, MaxLength(100)] string City);