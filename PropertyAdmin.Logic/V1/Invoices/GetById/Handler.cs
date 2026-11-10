namespace PropertyAdmin.Logic.V1.Invoices.GetById;

public sealed class GetByIdHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken) =>
        new(await invoices.GetByIdAsync(query.Id, query.OwnerScopeId, cancellationToken));
}
