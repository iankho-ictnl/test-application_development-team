namespace PropertyAdmin.Logic.V1.Invoices.GetAll;

public sealed class GetAllHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await invoices.GetAllAsync(query.OwnerScopeId, query.OwnerId, query.Status, cancellationToken));
}
