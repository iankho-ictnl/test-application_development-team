namespace PropertyAdmin.Logic.V1.VatRates.Create;

public sealed class CreateHandler(IVatRateService rates)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await rates.CreateAsync(command.Rate, cancellationToken));
}