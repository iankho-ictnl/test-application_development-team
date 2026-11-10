namespace PropertyAdmin.Logic.V1.Assets.Delete;

public sealed class DeleteHandler(IAssetService assets)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await assets.DeleteAsync(command.Id, cancellationToken));
}