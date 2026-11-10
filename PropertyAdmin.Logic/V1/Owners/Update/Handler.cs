namespace PropertyAdmin.Logic.V1.Owners.Update;

public sealed class UpdateHandler(IOwnerService owners)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await owners.UpdateAsync(command.Id, command.Owner, cancellationToken));
}