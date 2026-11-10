namespace PropertyAdmin.Logic.V1.Roles.Update;

public sealed class UpdateHandler(IRoleService roles)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await roles.UpdateAsync(command.Name, command.Role, cancellationToken));
}
