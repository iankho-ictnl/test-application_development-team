namespace PropertyAdmin.Logic.V1.Users.ForceSignOut;

public sealed class ForceSignOutHandler(IUserService users)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await users.ForceSignOutAsync(command.Id, cancellationToken));
}
