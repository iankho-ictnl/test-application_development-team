namespace PropertyAdmin.Logic.V1.Owners.GetAll;

public sealed class GetAllHandler(IOwnerService owners)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken)
    {
        var result = await owners.GetAllAsync(query.OwnerId, cancellationToken);
        return new Result(result);
    }
}