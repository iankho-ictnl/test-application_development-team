using System.ComponentModel.DataAnnotations;
using M = PropertyAdmin.Api.V1.Owners.OwnerMessages;

namespace PropertyAdmin.Api.V1.Owners.Update;

/// <summary>Owner fields as D-041: the five address fields, email and phone required; KvK and BTW optional.
/// Messages in the language of lang through OwnerMessages (D-049, T-127).</summary>
public sealed record Request(
    [Required(ErrorMessage = M.Required), MaxLength(200, ErrorMessage = M.MaxLength)] string Name,
    [Required(ErrorMessage = M.Required), EmailAddress(ErrorMessage = M.EmailFormat), MaxLength(200, ErrorMessage = M.MaxLength)] string Email,
    [Required(ErrorMessage = M.Required), MaxLength(50, ErrorMessage = M.MaxLength)] string Phone,
    [Required(ErrorMessage = M.Required), MaxLength(200, ErrorMessage = M.MaxLength)] string Street,
    [Required(ErrorMessage = M.Required), MaxLength(20, ErrorMessage = M.MaxLength)] string HouseNumber,
    [Required(ErrorMessage = M.Required), MaxLength(20, ErrorMessage = M.MaxLength)] string PostalCode,
    [Required(ErrorMessage = M.Required), MaxLength(100, ErrorMessage = M.MaxLength)] string City,
    [Required(ErrorMessage = M.Required), MaxLength(100, ErrorMessage = M.MaxLength)] string Country,
    [MaxLength(50, ErrorMessage = M.MaxLength)] string? KvkNumber,
    [MaxLength(50, ErrorMessage = M.MaxLength)] string? VatNumber) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        RequestValidation.Validate(this, validationContext);
}