namespace PropertyAdmin.Data;

/// <summary>The permissions stored as role claims; seeded by <see cref="Seeder"/>.</summary>
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
