using PropertyAdmin.Tests.Characterization;
using PropertyAdmin.Tests.V1.Assets;

namespace PropertyAdmin.Tests.V1.Invoices;

// S-34 (BUG-7, REQ-009, REQ-019): an invoice status filter given as an undefined number is refused
// with the normal 400 validation Problem Details.
[Collection(ApiCollection.Name)]
public sealed class InvoicesUndefinedEnumTests(ApiFactory api)
{
    [Fact]
    public async Task GetAll_with_undefined_status_returns_400_validation_problem()
    {
        var admin = await api.As("Admin");

        var response = await admin.GetAsync("/api/v1/invoices?status=7");

        await AssetsUndefinedEnumTests.ExpectEnumError(response, "Status");
    }
}
