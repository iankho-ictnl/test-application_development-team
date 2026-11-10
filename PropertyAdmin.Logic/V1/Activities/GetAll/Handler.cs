namespace PropertyAdmin.Logic.V1.Activities.GetAll;

public sealed class GetAllHandler(IActivityService activities)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await activities.GetAllAsync(query.OwnerScopeId, query.AssetId, query.From, query.To, cancellationToken));
}
