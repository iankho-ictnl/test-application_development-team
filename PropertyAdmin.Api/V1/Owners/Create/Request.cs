using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Owners.Create;

/// <summary>Owner fields as D-041: the five address fields, email and phone required; KvK and BTW optional.</summary>
public sealed record Request(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MaxLength(50)] string Phone,
    [Required, MaxLength(200)] string Street,
    [Required, MaxLength(20)] string HouseNumber,
    [Required, MaxLength(20)] string PostalCode,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(100)] string Country,
    [MaxLength(50)] string? KvkNumber,
    [MaxLength(50)] string? VatNumber) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        RequestValidation.Validate(this);
}