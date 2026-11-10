using Microsoft.EntityFrameworkCore;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Expenses.Models;
using PropertyAdmin.Logic.V1.VatRates;

namespace PropertyAdmin.Logic.V1.Expenses;

public sealed class ExpenseService(AppDbContext db, IVatRateService vatRates) : IExpenseService
{
    public async Task<IReadOnlyList<ExpenseModel>> GetAllAsync(int? ownerScopeId, int? assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken) =>
        await db.Expenses.AsNoTracking()
            .Where(expense => (ownerScopeId == null || expense.Asset.OwnerId == ownerScopeId)
                && (assetId == null || expense.AssetId == assetId)
                && (from == null || expense.Date >= from)
                && (to == null || expense.Date <= to))
            .OrderByDescending(expense => expense.Date).ThenByDescending(expense => expense.Id)
            .Select(expense => new ExpenseModel(expense.Id, expense.AssetId, expense.Asset.Name, expense.Date, expense.Description,
                expense.Category, expense.AmountExclVat, expense.VatRateName, expense.VatPercentage, expense.Reinvoiceable, expense.InvoiceId))
            .ToListAsync(cancellationToken);

    public async Task<ExpenseModel?> GetByIdAsync(int id, int? ownerScopeId, CancellationToken cancellationToken) =>
        await db.Expenses.AsNoTracking()
            .Where(expense => expense.Id == id && (ownerScopeId == null || expense.Asset.OwnerId == ownerScopeId))
            .Select(expense => new ExpenseModel(expense.Id, expense.AssetId, expense.Asset.Name, expense.Date, expense.Description,
                expense.Category, expense.AmountExclVat, expense.VatRateName, expense.VatPercentage, expense.Reinvoiceable, expense.InvoiceId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ExpenseModel> CreateAsync(ExpenseSaveModel model, CancellationToken cancellationToken)
    {
        var expense = new Expense();
        var assetName = await ApplyAsync(expense, model, cancellationToken);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(expense, assetName);
    }

    public async Task<ExpenseModel?> UpdateAsync(int id, ExpenseSaveModel model, CancellationToken cancellationToken)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (expense is null) return null;
        if (expense.InvoiceId is not null)
            throw new BusinessRuleException("Deze kosten zijn gefactureerd en kunnen niet worden gewijzigd.");

        var assetName = await ApplyAsync(expense, model, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToModel(expense, assetName);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (expense is null) return false;
        if (expense.InvoiceId is not null)
            throw new BusinessRuleException("Deze kosten zijn gefactureerd en kunnen niet worden verwijderd.");

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> ApplyAsync(Expense expense, ExpenseSaveModel model, CancellationToken cancellationToken)
    {
        var assetName = await db.Assets.AsNoTracking()
            .Where(asset => asset.Id == model.AssetId)
            .Select(asset => asset.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleException("Object bestaat niet.");

        expense.AssetId = model.AssetId;
        expense.Date = model.Date;
        expense.Description = model.Description.Trim();
        expense.Category = model.Category;
        expense.AmountExclVat = Money.Round(model.AmountExclVat);
        expense.VatRateName = model.VatRateName;
        expense.VatPercentage = await vatRates.GetPercentageAsync(model.VatRateName, model.Date, cancellationToken);
        expense.Reinvoiceable = model.Reinvoiceable;
        return assetName;
    }

    private static ExpenseModel ToModel(Expense expense, string assetName) =>
        new(expense.Id, expense.AssetId, assetName, expense.Date, expense.Description, expense.Category, expense.AmountExclVat,
            expense.VatRateName, expense.VatPercentage, expense.Reinvoiceable, expense.InvoiceId);
}
