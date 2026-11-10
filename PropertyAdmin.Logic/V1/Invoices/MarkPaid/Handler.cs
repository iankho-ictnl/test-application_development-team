namespace PropertyAdmin.Logic.V1.Invoices.MarkPaid;

public sealed class MarkPaidHandler(IInvoiceService invoices)
{
    public async Task<Result> HandleAsync(Command command, CancellationToken cancellationToken) =>
        new(await invoices.MarkPaidAsync(command.Id, command.PaidDate, cancellationToken));
}
