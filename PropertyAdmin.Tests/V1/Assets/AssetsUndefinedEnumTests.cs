using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Assets;

// S-34 (BUG-7, REQ-003, REQ-019): an asset type sent as an undefined number is refused with the
// normal 400 validation Problem Details and is never stored.
[Collection(ApiCollection.Name)]
public sealed class AssetsUndefinedEnumTests(ApiFactory api)
{
    [Theory]
    [InlineData("/api/v1/assets")]
    [InlineData("/api/assets")]
    public async Task Create_with_undefined_type_returns_400_validation_problem(string route)
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("S34 owner"));

        var response = await admin.PostAsJsonAsync(route, new { ownerId, type = 7, name = "House", address = "Road 1", postalCode = "1000 AA", city = "City" });

        await ExpectEnumError(response, "Type");
    }

    [Fact]
    public async Task Update_with_undefined_type_returns_400_validation_problem()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("S34 owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("S34 house"));

        var response = await admin.PutAsJsonAsync($"/api/v1/assets/{assetId}", new { ownerId, type = 7, name = "House", address = "Road 1", postalCode = "1000 AA", city = "City" });

        await ExpectEnumError(response, "Type");
        var stored = await Expect(await admin.GetAsync($"/api/v1/assets/{assetId}"), System.Net.HttpStatusCode.OK);
        Assert.Equal(0, stored.GetProperty("type").GetInt32());
    }

    internal static async Task ExpectEnumError(HttpResponseMessage response, string field)
    {
        await ExpectValidationProblem(response);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Contains(json.GetProperty("errors").EnumerateObject(), e => string.Equals(e.Name, field, StringComparison.OrdinalIgnoreCase));
    }
}
