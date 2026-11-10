namespace PropertyAdmin.Logic.V1.Owners.Create;

public sealed class CreateHandler(IOwnerService owners)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await owners.CreateAsync(command.Owner, cancellationToken));
}