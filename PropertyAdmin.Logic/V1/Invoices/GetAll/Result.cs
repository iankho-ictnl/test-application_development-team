using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Logic.V1.Invoices.GetAll;

public sealed record Result(IReadOnlyList<InvoiceModel> Invoices);
