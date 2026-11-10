namespace PropertyAdmin.Logic.V1.VatRates.GetById;

public sealed class GetByIdHandler(IVatRateService rates)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await rates.GetByIdAsync(query.Id, cancellationToken));
}