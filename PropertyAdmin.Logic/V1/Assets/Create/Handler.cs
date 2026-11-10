namespace PropertyAdmin.Logic.V1.Assets.Create;

public sealed class CreateHandler(IAssetService assets)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await assets.CreateAsync(command.Asset, cancellationToken));
}