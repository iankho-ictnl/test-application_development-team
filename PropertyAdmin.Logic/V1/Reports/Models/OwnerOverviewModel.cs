namespace PropertyAdmin.Logic.V1.Reports.Models;

public sealed record OwnerOverviewModel(
    int OwnerId,
    string OwnerName,
    int AssetCount,
    decimal Income,
    decimal Expenses,
    int OpenInvoiceCount,
    decimal OpenInvoiceAmount);
