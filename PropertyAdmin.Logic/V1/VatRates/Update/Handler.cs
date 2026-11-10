namespace PropertyAdmin.Logic.V1.VatRates.Update;

public sealed class UpdateHandler(IVatRateService rates)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await rates.UpdateAsync(command.Id, command.Rate, cancellationToken));
}