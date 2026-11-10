namespace PropertyAdmin.Logic.V1.Owners.Models;

public sealed record OwnerModel(
    int Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? PostalCode,
    string? City);