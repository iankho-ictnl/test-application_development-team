namespace PropertyAdmin.Api.V1.Owners.GetById;

public sealed record Response(
    int Id,
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