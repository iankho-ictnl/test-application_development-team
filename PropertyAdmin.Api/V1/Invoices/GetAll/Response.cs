using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.V1.Invoices.GetAll;

public sealed record Response(
    int Id,
    string Number,
    int OwnerId,
    string OwnerName,
    DateOnly Date,
    DateOnly DueDate,
    InvoiceStatus Status,
    DateOnly? PaidDate,
    decimal TotalExclVat,
    decimal TotalVat,
    decimal TotalInclVat,
    IReadOnlyList<LineResponse> Lines,
    IReadOnlyList<VatSummaryResponse> VatSummary);

public sealed record LineResponse(string Description, decimal AmountExclVat, decimal VatPercentage, decimal VatAmount, decimal AmountInclVat);

public sealed record VatSummaryResponse(decimal VatPercentage, decimal Base, decimal VatAmount);
