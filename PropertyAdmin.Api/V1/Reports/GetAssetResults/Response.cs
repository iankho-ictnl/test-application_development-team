namespace PropertyAdmin.Api.V1.Reports.GetAssetResults;

public sealed record Response(int AssetId, string AssetName, string OwnerName, decimal Income, decimal Expenses, decimal Result);
