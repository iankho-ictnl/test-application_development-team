namespace PropertyAdmin.Logic.V1.Assets.GetAll;

public sealed class GetAllHandler(IAssetService assets)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await assets.GetAllAsync(query.OwnerScopeId, query.OwnerId, cancellationToken));
}