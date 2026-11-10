namespace PropertyAdmin.Api.V1.Reports.GetOwnerOverview;

public sealed record Response(
    int OwnerId,
    string OwnerName,
    int AssetCount,
    decimal Income,
    decimal Expenses,
    int OpenInvoiceCount,
    decimal OpenInvoiceAmount);
