namespace PropertyAdmin.Logic.V1.Invoices.GetById;

public sealed record Query(int Id, int? OwnerScopeId);
