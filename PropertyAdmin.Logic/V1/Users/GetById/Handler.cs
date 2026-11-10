namespace PropertyAdmin.Logic.V1.Users.GetById;

public sealed class GetByIdHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await users.GetByIdAsync(query.Id, cancellationToken));
}
