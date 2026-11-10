namespace PropertyAdmin.Api.V1.Owners.GetAll;

public sealed record Response(
    int Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? PostalCode,
    string? City);