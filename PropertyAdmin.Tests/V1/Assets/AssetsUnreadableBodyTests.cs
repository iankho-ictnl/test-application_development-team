using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;

namespace PropertyAdmin.Tests.V1.Assets;

// BUG-7 (D-043, REQ-003, REQ-019): an enum sent as text cannot be read; the API answers with the
// normal 400 validation Problem Details and reveals no .NET type name or exception text (D-022).
[Collection(ApiCollection.Name)]
public sealed class AssetsUnreadableBodyTests(ApiFactory api)
{
    [Theory]
    [InlineData("/api/v1/assets")]
    [InlineData("/api/assets")]
    public async Task Create_with_enum_as_text_returns_400_problem_details_without_internal_details(string route)
    {
        var admin = await api.As("Admin");
        var owner = await admin.PostAsJsonAsync("/api/v1/owners", new
        {
            name = api.Unique("Bug7 owner"),
            email = "owner@example.com",
            phone = "0101234567",
            address = "Straat 1",
            postalCode = "1234 AB",
            city = "Utrecht",
        });
        Assert.Equal(HttpStatusCode.Created, owner.StatusCode);
        var ownerId = (await owner.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();

        var response = await admin.PostAsJsonAsync(route, new
        {
            ownerId,
            type = "Home",
            name = "House",
            address = "Road 1",
            postalCode = "1000 AA",
            city = "City",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PropertyAdmin", body);
        Assert.DoesNotContain("could not be converted", body);
        Assert.DoesNotContain("System.", body);
        Assert.Contains("$.type", body);
    }
}
