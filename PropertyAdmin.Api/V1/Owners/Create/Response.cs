namespace PropertyAdmin.Api.V1.Owners.Create;

public sealed record Response(
    int Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? PostalCode,
    string? City);