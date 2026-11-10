using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;

namespace PropertyAdmin.Tests.V1.Owners;

[Collection(ApiCollection.Name)]
public sealed class OwnersRouteTests(ApiFactory api)
{
    [Theory]
    [InlineData("/api/v1/owners")]
    [InlineData("/api/owners")]
    public async Task Create_returns_a_location_under_the_route_it_was_called_on(string route)
    {
        var admin = await api.As("Admin");

        var response = await admin.PostAsJsonAsync(route, new
        {
            name = api.Unique("Location owner"),
            email = "owner@example.com",
            phone = "0101234567",
            address = "Straat 1",
            postalCode = "1234 AB",
            city = "Utrecht",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal($"{route}/{id}", response.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(response.Headers.Location)).StatusCode);
    }
}
