namespace PropertyAdmin.Logic.V1.Users.Delete;

public sealed class DeleteHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await users.DeleteAsync(command.Id, cancellationToken));
}
