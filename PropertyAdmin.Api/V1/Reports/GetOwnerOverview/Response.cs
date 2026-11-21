namespace PropertyAdmin.Api.V1.Reports.GetOwnerOverview;

public sealed record Response(
    int OwnerId,
    string OwnerName,
    int AssetCount,
    decimal Income,
    decimal Expenses,
    int OpenInvoiceCount,
    decimal OpenInvoiceAmount,
    IReadOnlyList<OpenInvoiceResponse> OpenInvoices);

/// <summary>An open invoice of the owner (Q-117 names: id, number, date, dueDate, totalInclVat, overdue).</summary>
public sealed record OpenInvoiceResponse(int Id, string Number, DateOnly Date, DateOnly DueDate, decimal TotalInclVat, bool Overdue);
