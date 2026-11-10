namespace PropertyAdmin.Logic.V1.Roles.GetAll;

public sealed class GetAllHandler(IRoleService roles)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await roles.GetAllAsync(cancellationToken));
}
