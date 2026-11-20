namespace PropertyAdmin.Logic.V1.Invoices.Models;

public sealed record InvoiceOwnerModel(
    string Name,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City,
    string? Country,
    string? KvkNumber,
    string? VatNumber);
