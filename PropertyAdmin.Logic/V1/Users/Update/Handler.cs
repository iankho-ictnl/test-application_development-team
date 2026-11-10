namespace PropertyAdmin.Logic.V1.Users.Update;

public sealed class UpdateHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await users.UpdateAsync(command.Id, command.User, cancellationToken));
}
