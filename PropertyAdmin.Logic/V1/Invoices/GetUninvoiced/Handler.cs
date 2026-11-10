namespace PropertyAdmin.Logic.V1.Invoices.GetUninvoiced;

public sealed class GetUninvoicedHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await invoices.GetUninvoicedAsync(query.OwnerId, cancellationToken));
}
