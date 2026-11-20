namespace PropertyAdmin.Logic.V1.Owners.Models;

public sealed record OwnerSaveModel(
    string Name,
    string? Email,
    string? Phone,
    string Street,
    string HouseNumber,
    string PostalCode,
    string City,
    string Country,
    string? KvkNumber,
    string? VatNumber);