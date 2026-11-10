using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Owners.Create;

public sealed record Request(
    [Required, MaxLength(200)] string Name,
    [EmailAddress, MaxLength(200)] string? Email,
    [MaxLength(50)] string? Phone,
    [MaxLength(200)] string? Address,
    [MaxLength(20)] string? PostalCode,
    [MaxLength(100)] string? City);