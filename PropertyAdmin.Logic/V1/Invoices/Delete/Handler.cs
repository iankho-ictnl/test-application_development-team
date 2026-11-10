namespace PropertyAdmin.Logic.V1.Invoices.Delete;

public sealed class DeleteHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await invoices.DeleteAsync(command.Id, cancellationToken));
}
