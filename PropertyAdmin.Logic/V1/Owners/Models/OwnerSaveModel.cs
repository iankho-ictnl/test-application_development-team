namespace PropertyAdmin.Logic.V1.Owners.Models;

public sealed record OwnerSaveModel(
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? PostalCode,
    string? City);