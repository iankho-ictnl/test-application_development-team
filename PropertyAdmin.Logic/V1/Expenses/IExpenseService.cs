using PropertyAdmin.Logic.V1.Expenses.Models;

namespace PropertyAdmin.Logic.V1.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseModel>> GetAllAsync(int? ownerScopeId, int? assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<ExpenseModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken);
    Task<ExpenseModel> CreateAsync(ExpenseSaveModel model, CancellationToken cancellationToken);
    Task<ExpenseModel?> UpdateAsync(int id, ExpenseSaveModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
