namespace PropertyAdmin.Logic.V1.Reports.Models;

public sealed record OwnerOverviewModel(
    int OwnerId,
    string OwnerName,
    int AssetCount,
    decimal Income,
    decimal Expenses,
    int OpenInvoiceCount,
    decimal OpenInvoiceAmount,
    IReadOnlyList<OpenInvoiceModel> OpenInvoices);

/// <summary>An open invoice in the owner overview; Overdue is true when the due date is before today (D-046).</summary>
public sealed record OpenInvoiceModel(int Id, string Number, DateOnly Date, DateOnly DueDate, decimal TotalInclVat, bool Overdue);
