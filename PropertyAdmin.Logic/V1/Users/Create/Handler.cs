namespace PropertyAdmin.Logic.V1.Users.Create;

public sealed class CreateHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await users.CreateAsync(command.User, cancellationToken));
}
