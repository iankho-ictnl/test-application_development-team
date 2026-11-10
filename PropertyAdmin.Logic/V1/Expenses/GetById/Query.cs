namespace PropertyAdmin.Logic.V1.Expenses.GetById;

public sealed record Query(int Id, int? OwnerScopeId);
