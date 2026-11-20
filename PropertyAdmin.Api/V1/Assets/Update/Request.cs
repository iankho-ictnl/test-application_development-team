using System.ComponentModel.DataAnnotations;
using M = PropertyAdmin.Api.V1.Owners.OwnerMessages;

namespace PropertyAdmin.Api.V1.Assets.Update;

/// <summary>Asset fields as D-041: the five address fields required (trimmed, not empty), foreign addresses accepted.
/// Messages in the language of lang through OwnerMessages (D-049, D-052).</summary>
public sealed record Request(
    [Range(1, int.MaxValue, ErrorMessage = M.InvalidChoice)] int OwnerId,
    [EnumDataType(typeof(AssetType), ErrorMessage = M.InvalidChoice)] AssetType Type,
    [Required(ErrorMessage = M.Required), MaxLength(200, ErrorMessage = M.MaxLength)] string Name,
    [Required(ErrorMessage = M.Required), MaxLength(200, ErrorMessage = M.MaxLength)] string Street,
    [Required(ErrorMessage = M.Required), MaxLength(20, ErrorMessage = M.MaxLength)] string HouseNumber,
    [Required(ErrorMessage = M.Required), MaxLength(20, ErrorMessage = M.MaxLength)] string PostalCode,
    [Required(ErrorMessage = M.Required), MaxLength(100, ErrorMessage = M.MaxLength)] string City,
    [Required(ErrorMessage = M.Required), MaxLength(100, ErrorMessage = M.MaxLength)] string Country);