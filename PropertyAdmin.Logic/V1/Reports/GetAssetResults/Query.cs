namespace PropertyAdmin.Logic.V1.Reports.GetAssetResults;

public sealed record Query(DateOnly From, DateOnly To, int? OwnerScopeId);
