namespace PropertyAdmin.Logic.V1.Users.GetAll;

public sealed class GetAllHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await users.GetAllAsync(cancellationToken));
}
