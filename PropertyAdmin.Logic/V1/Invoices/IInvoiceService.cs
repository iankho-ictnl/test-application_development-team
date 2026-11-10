using PropertyAdmin.Contracts;
using PropertyAdmin.Logic.V1.Invoices.Models;

namespace PropertyAdmin.Logic.V1.Invoices;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceModel>> GetAllAsync(int? ownerScopeId, int? ownerId, InvoiceStatus? status, CancellationToken cancellationToken);
    Task<InvoiceModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken);
    Task<UninvoicedModel> GetUninvoicedAsync(int ownerId, CancellationToken cancellationToken);
    Task<InvoiceModel> CreateAsync(InvoiceCreateModel model, CancellationToken cancellationToken);
    Task<bool> MarkPaidAsync(int id, DateOnly paidDate, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
