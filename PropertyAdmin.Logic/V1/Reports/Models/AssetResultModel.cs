namespace PropertyAdmin.Logic.V1.Reports.Models;

public sealed record AssetResultModel(int AssetId, string AssetName, int OwnerId, string OwnerName, decimal Income, decimal Expenses)
{
    public decimal Result => Income - Expenses;
}
