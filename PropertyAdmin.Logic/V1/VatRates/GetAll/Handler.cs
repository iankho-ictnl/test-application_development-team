namespace PropertyAdmin.Logic.V1.VatRates.GetAll;

public sealed class GetAllHandler(IVatRateService rates)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await rates.GetAllAsync(cancellationToken));
}