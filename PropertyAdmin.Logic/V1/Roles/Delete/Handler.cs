namespace PropertyAdmin.Logic.V1.Roles.Delete;

public sealed class DeleteHandler(IRoleService roles)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await roles.DeleteAsync(command.Name, cancellationToken));
}
