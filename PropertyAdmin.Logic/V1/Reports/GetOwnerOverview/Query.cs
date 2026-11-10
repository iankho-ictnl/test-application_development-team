namespace PropertyAdmin.Logic.V1.Reports.GetOwnerOverview;

public sealed record Query(DateOnly From, DateOnly To, int? OwnerScopeId);
