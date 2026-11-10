namespace PropertyAdmin.Logic.V1.Roles.Create;

public sealed class CreateHandler(IRoleService roles)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken)
    {
        await roles.CreateAsync(command.Role, cancellationToken);
        return new Result();
    }
}
