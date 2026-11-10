namespace PropertyAdmin.Api.Auth;

/// <summary>The permission claim and the policy names; the same values as the role claims stored by the Data project.</summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    public const string Users = "Users";
    public const string VatRates = "VatRates";
    public const string OwnersAssets = "OwnersAssets";
    public const string ExpensesActivities = "ExpensesActivities";
    public const string Invoices = "Invoices";
    public const string Reports = "Reports";
    public const string ReadAll = "ReadAll";
    public const string ReadOwn = "ReadOwn";

    public static readonly IReadOnlyList<string> All =
        [Users, VatRates, OwnersAssets, ExpensesActivities, Invoices, Reports, ReadAll, ReadOwn];
}
