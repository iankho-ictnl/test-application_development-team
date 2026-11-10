namespace PropertyAdmin.Logic.V1.Invoices.Create;

public sealed class CreateHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await invoices.CreateAsync(command.Invoice, cancellationToken));
}
