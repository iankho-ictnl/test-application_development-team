using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.V1.Invoices.GetAll;

public sealed record Request(int? OwnerId, InvoiceStatus? Status);
