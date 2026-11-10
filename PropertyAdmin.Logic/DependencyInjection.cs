using Microsoft.Extensions.DependencyInjection;
using PropertyAdmin.Data;
using PropertyAdmin.Logic.V1.Owners;
using PropertyAdmin.Logic.V1.Owners.Create;
using PropertyAdmin.Logic.V1.Owners.Delete;
using PropertyAdmin.Logic.V1.Owners.GetById;
using PropertyAdmin.Logic.V1.Owners.GetAll;
using PropertyAdmin.Logic.V1.Owners.Update;

namespace PropertyAdmin.Logic;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the logic layer: per feature the service and per use case the handler, as plain
    /// classes (no MediatR, D-026). The features are added under V1/ by T-033 to T-036.
    /// </summary>
    public static IServiceCollection AddLogic(this IServiceCollection services, string? connectionString)
    {
        services.AddData(connectionString);
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<CreateHandler>();
        services.AddScoped<DeleteHandler>();
        services.AddScoped<GetByIdHandler>();
        services.AddScoped<GetAllHandler>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.IAssetService, PropertyAdmin.Logic.V1.Assets.AssetService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Assets.Delete.DeleteHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.IVatRateService, PropertyAdmin.Logic.V1.VatRates.VatRateService>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.VatRates.Delete.DeleteHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.IExpenseService, PropertyAdmin.Logic.V1.Expenses.ExpenseService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Expenses.Delete.DeleteHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.IActivityService, PropertyAdmin.Logic.V1.Activities.ActivityService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Activities.Delete.DeleteHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.IInvoiceService, PropertyAdmin.Logic.V1.Invoices.InvoiceService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.GetUninvoiced.GetUninvoicedHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.MarkPaid.MarkPaidHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.Delete.DeleteHandler>();
        services.AddSingleton<PropertyAdmin.Logic.V1.Invoices.GetPdf.IInvoicePdfRenderer, PropertyAdmin.Logic.V1.Invoices.GetPdf.InvoicePdfRenderer>();
        services.AddScoped<PropertyAdmin.Logic.V1.Invoices.GetPdf.GetPdfHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Reports.IReportService, PropertyAdmin.Logic.V1.Reports.ReportService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Reports.GetAssetResults.GetAssetResultsHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Reports.GetOwnerOverview.GetOwnerOverviewHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.IAuthService, PropertyAdmin.Logic.V1.Auth.AuthService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.Login.LoginHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.LoginTwoFactor.LoginTwoFactorHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.GetTwoFactor.GetTwoFactorHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.EnableTwoFactor.EnableTwoFactorHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.DisableTwoFactor.DisableTwoFactorHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Auth.ChangePassword.ChangePasswordHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.IUserService, PropertyAdmin.Logic.V1.Users.UserService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.GetById.GetByIdHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.ResetTwoFactor.ResetTwoFactorHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.ForceSignOut.ForceSignOutHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Users.Delete.DeleteHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Roles.IRoleService, PropertyAdmin.Logic.V1.Roles.RoleService>();
        services.AddScoped<PropertyAdmin.Logic.V1.Roles.GetAll.GetAllHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Roles.Create.CreateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Roles.Update.UpdateHandler>();
        services.AddScoped<PropertyAdmin.Logic.V1.Roles.Delete.DeleteHandler>();
        return services;
    }
}
