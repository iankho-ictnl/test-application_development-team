namespace PropertyAdmin.Logic.V1.Activities.GetById;

public sealed class GetByIdHandler(IActivityService activities)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await activities.GetByIdAsync(query.Id, query.OwnerScopeId, cancellationToken));
}
