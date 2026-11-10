namespace PropertyAdmin.Logic.V1.Assets.GetById;

public sealed class GetByIdHandler(IAssetService assets)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await assets.GetByIdAsync(query.Id, query.OwnerScopeId, cancellationToken));
}