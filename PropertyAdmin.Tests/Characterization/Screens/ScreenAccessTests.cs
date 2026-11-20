using System.Net;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>
/// TC-11: every screen per role follows the authorization matrix in architecture.md: allowed
/// roles get the page (200), signed-in roles without the permission get the access-denied page.
/// </summary>
[Collection(FrontendCollection.Name)]
[Trait("Requirement", "REQ-012")]
[Trait("Requirement", "REQ-019")]
[Trait("TestCase", "TC-11")]
public sealed class ScreenAccessTests(FrontendFactory web)
{
    private const string Users = "Users", VatRates = "VatRates", OwnersAssets = "OwnersAssets",
        ExpensesActivities = "ExpensesActivities", Invoices = "Invoices", Reports = "Reports", ReadAll = "ReadAll";

    // The authorization matrix of architecture.md, per role.
    private static readonly Dictionary<string, string[]> Matrix = new()
    {
        ["Admin"] = [Users, VatRates, OwnersAssets, ExpensesActivities, Invoices, Reports, ReadAll],
        ["Manager"] = [OwnersAssets, ExpensesActivities, Invoices, Reports, ReadAll],
        ["Staff"] = [ExpensesActivities, ReadAll],
        ["Finance"] = [VatRates, Invoices, Reports, ReadAll],
        ["Viewer"] = [Reports, ReadAll],
        ["Owner"] = [],
    };

    // null: every signed-in user. Profile is last: its first visit ends the API session.
    private static readonly (string Url, string? Permission)[] Screens =
    [
        ("/", null),
        ("/Owners", ReadAll), ("/Owners/Details/{owner}", ReadAll), ("/Owners/Edit", OwnersAssets), ("/Owners/Edit/{owner}", OwnersAssets),
        ("/Assets", ReadAll), ("/Assets/Details/{asset}", ReadAll), ("/Assets/Edit", OwnersAssets), ("/Assets/Edit/{asset}", OwnersAssets),
        ("/Expenses", ReadAll), ("/Expenses/Edit", ExpensesActivities),
        ("/Activities", ReadAll), ("/Activities/Edit", ExpensesActivities),
        ("/VatRates", VatRates),
        ("/Invoices", ReadAll), ("/Invoices/Details/{invoice}", ReadAll), ("/Invoices/Create", Invoices), ("/Invoices/Details/{invoice}?handler=Pdf", ReadAll),
        ("/Reports", Reports),
        ("/Admin/Users", Users), ("/Admin/Users/Edit", Users), ("/Admin/Roles", Users),
        ("/Profile", null),
    ];

    public static TheoryData<string> Roles => new(FrontendFactory.Roles);

    [Theory]
    [MemberData(nameof(Roles))]
    public async Task Every_screen_follows_the_authorization_matrix(string role)
    {
        var data = await web.DataAsync();
        var browser = await web.SignInAs(role);
        var failures = new List<string>();

        foreach (var (template, permission) in Screens)
        {
            var url = template.Replace("{owner}", $"{data.OwnerId}").Replace("{asset}", $"{data.AssetId}").Replace("{invoice}", $"{data.InvoiceId}");
            var allowed = permission is null || Matrix[role].Contains(permission);
            var response = await browser.GetAsync(url);
            var location = response.Headers.Location?.OriginalString ?? "";
            var ok = allowed
                ? response.StatusCode == HttpStatusCode.OK
                : response.StatusCode == HttpStatusCode.Redirect && location.Contains("/Account/AccessDenied", StringComparison.Ordinal);
            if (!ok) failures.Add($"{url}: expected {(allowed ? "200" : "access denied")}, got {(int)response.StatusCode} {location}");
        }

        Assert.Empty(failures);
    }
}
