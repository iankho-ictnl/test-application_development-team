using PropertyAdmin.Data;

namespace PropertyAdmin.Logic.V1.Invoices.GetAll;

public sealed record Query(int? OwnerScopeId, int? OwnerId, InvoiceStatus? Status);
