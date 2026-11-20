using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using PropertyAdmin.Tests.V1.Assets;

namespace PropertyAdmin.Tests.V1.Activities;

// S-34 (BUG-7, REQ-005, REQ-019): an activity kind or pricing type sent as an undefined number is
// refused with the normal 400 validation Problem Details.
[Collection(ApiCollection.Name)]
public sealed class ActivitiesUndefinedEnumTests(ApiFactory api)
{
    [Theory]
    [InlineData(9, 0, "Kind")]
    [InlineData(0, 9, "PricingType")]
    public async Task Create_with_undefined_enum_returns_400_validation_problem(int kind, int pricingType, string field)
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("S34 owner")), api.Unique("S34 house"));

        var response = await admin.PostAsJsonAsync("/api/v1/activities", Activity(assetId, kind, pricingType));

        await AssetsUndefinedEnumTests.ExpectEnumError(response, field);
    }

    [Theory]
    [InlineData(9, 0, "Kind")]
    [InlineData(0, 9, "PricingType")]
    public async Task Update_with_undefined_enum_returns_400_validation_problem(int kind, int pricingType, string field)
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("S34 owner")), api.Unique("S34 house"));
        var id = await admin.CreateActivity(assetId);

        var response = await admin.PutAsJsonAsync($"/api/v1/activities/{id}", Activity(assetId, kind, pricingType));

        await AssetsUndefinedEnumTests.ExpectEnumError(response, field);
    }

    private static object Activity(int assetId, int kind, int pricingType) =>
        new { assetId, date = "2026-03-02", kind, description = "Administratie", pricingType, hours = 2.5m, hourlyRate = 40m, fixedPrice = 99m, vatRateName = "Hoog" };
}
