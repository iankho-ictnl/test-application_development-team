namespace PropertyAdmin.Logic.V1.Owners.Delete;

public sealed class DeleteHandler(IOwnerService owners)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await owners.DeleteAsync(command.Id, cancellationToken));
}