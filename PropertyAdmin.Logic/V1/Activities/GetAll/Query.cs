namespace PropertyAdmin.Logic.V1.Activities.GetAll;

public sealed record Query(int? OwnerScopeId, int? AssetId, DateOnly? From, DateOnly? To);
