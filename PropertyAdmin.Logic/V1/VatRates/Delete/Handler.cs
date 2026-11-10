namespace PropertyAdmin.Logic.V1.VatRates.Delete;

public sealed class DeleteHandler(IVatRateService rates)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await rates.DeleteAsync(command.Id, cancellationToken));
}