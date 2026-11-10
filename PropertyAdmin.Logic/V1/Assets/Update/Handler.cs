namespace PropertyAdmin.Logic.V1.Assets.Update;

public sealed class UpdateHandler(IAssetService assets)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await assets.UpdateAsync(command.Id, command.Asset, cancellationToken));
}