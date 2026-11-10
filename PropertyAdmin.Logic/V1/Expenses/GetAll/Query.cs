namespace PropertyAdmin.Logic.V1.Expenses.GetAll;

public sealed record Query(int? OwnerScopeId, int? AssetId, DateOnly? From, DateOnly? To);
