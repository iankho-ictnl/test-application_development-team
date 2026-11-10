namespace PropertyAdmin.Logic.V1.Owners.GetById;

public sealed class GetByIdHandler(IOwnerService owners)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await owners.GetByIdAsync(query.Id, query.OwnerId, cancellationToken));
}