namespace PropertyAdmin.Logic.V1.Invoices.GetPdf;

public sealed class GetPdfHandler(IInvoiceService invoices, IInvoicePdfRenderer renderer)
{
    public async Task<Result> HandleAsync(Query query, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(query.Id, query.OwnerScopeId, cancellationToken);
        if (invoice is null) return new Result(null, null);
        var owner = await invoices.GetOwnerAsync(invoice.OwnerId, cancellationToken);
        return new Result($"Factuur-{invoice.Number}.pdf", renderer.Render(invoice, owner));
    }
}
