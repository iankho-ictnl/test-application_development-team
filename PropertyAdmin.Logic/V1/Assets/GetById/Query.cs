namespace PropertyAdmin.Logic.V1.Assets.GetById;

public sealed record Query(int Id, int? OwnerScopeId);