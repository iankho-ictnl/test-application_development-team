using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Assets;

/// <summary>
/// T-053 (B15, REQ-003, BUG-5; D-041, D-049, D-052): the asset has five required address fields
/// (street, house number, postal code, city, country), foreign addresses are accepted, and every
/// validation and business-rule message comes in the language of <c>lang</c> (Dutch by default).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AssetFieldsApiTests(ApiFactory api)
{
    private const string RequiredNl = "Dit veld is verplicht.";
    private const string RequiredEn = "This field is required.";

    private static JsonObject Body(int ownerId) => JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(AssetBody(ownerId, "House")))!.AsObject();

    private static string Route(string path, string? lang) => lang is null ? path : $"{path}?lang={lang}";

    [Theory]
    [InlineData("street", "Street", "nl-NL", RequiredNl)]
    [InlineData("houseNumber", "HouseNumber", "en-US", RequiredEn)]
    [InlineData("postalCode", "PostalCode", null, RequiredNl)]
    [InlineData("city", "City", "en-US", RequiredEn)]
    [InlineData("country", "Country", "nl-NL", RequiredNl)]
    public async Task Create_without_an_address_field_gives_400_at_that_field_in_the_language_of_lang(string json, string field, string? lang, string message)
    {
        var admin = await api.As("Admin");
        var body = Body(await admin.CreateOwner(api.Unique("B15 owner")));
        body.Remove(json);

        var errors = (await ExpectProblem(await admin.PostAsJsonAsync(Route("/api/v1/assets", lang), body), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(message, errors.GetProperty(field)[0].GetString());
    }

    [Theory]
    [InlineData("country", "Country")]
    [InlineData("houseNumber", "HouseNumber")]
    public async Task Update_with_a_blank_address_field_gives_400_at_that_field(string json, string field)
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("B15 owner"));
        var assetId = await admin.CreateAsset(ownerId, api.Unique("B15 house"));
        var body = Body(ownerId);
        body[json] = "   ";

        var errors = (await ExpectProblem(await admin.PutAsJsonAsync(Route($"/api/v1/assets/{assetId}", "en-US"), body), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(RequiredEn, errors.GetProperty(field)[0].GetString());
    }

    [Fact]
    public async Task Create_too_long_country_gives_the_max_length_message_in_english()
    {
        var admin = await api.As("Admin");
        var body = Body(await admin.CreateOwner(api.Unique("B15 owner")));
        body["country"] = new string('x', 101);

        var errors = (await ExpectProblem(await admin.PostAsJsonAsync(Route("/api/v1/assets", "en-US"), body), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal("This field can contain at most 100 characters.", errors.GetProperty("Country")[0].GetString());
    }

    [Fact]
    public async Task Create_and_update_accept_a_foreign_address_and_return_the_five_fields_trimmed()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("B15 owner"));
        var body = Body(ownerId);
        body["street"] = " Rue Haute ";
        body["houseNumber"] = "12A";
        body["postalCode"] = "1000";
        body["city"] = "Bruxelles";
        body["country"] = " Belgique ";

        var created = await Expect(await admin.PostAsJsonAsync("/api/v1/assets", body), HttpStatusCode.Created);
        HasShape(created, AssetShape);
        Assert.Equal(("Rue Haute", "12A", "1000", "Bruxelles", "Belgique"), (
            created.GetProperty("street").GetString(), created.GetProperty("houseNumber").GetString(),
            created.GetProperty("postalCode").GetString(), created.GetProperty("city").GetString(), created.GetProperty("country").GetString()));

        var id = created.GetProperty("id").GetInt32();
        body["country"] = "Deutschland";
        Assert.Equal("Deutschland", (await Expect(await admin.PutAsJsonAsync($"/api/v1/assets/{id}", body), HttpStatusCode.OK)).GetProperty("country").GetString());
        var read = await Expect(await admin.GetAsync($"/api/v1/assets/{id}"), HttpStatusCode.OK);
        Assert.Equal(("Rue Haute", "Deutschland"), (read.GetProperty("street").GetString(), read.GetProperty("country").GetString()));
    }

    [Theory]
    [InlineData("nl-NL", "Eigenaar bestaat niet.", "Kies een geldige waarde.")]
    [InlineData("en-US", "The owner does not exist.", "Choose a valid value.")]
    public async Task Owner_messages_follow_lang(string lang, string unknownOwner, string invalidChoice)
    {
        var admin = await api.As("Admin");

        var unknown = await ExpectProblem(await admin.PostAsJsonAsync(Route("/api/v1/assets", lang), AssetBody(999999, "X")), HttpStatusCode.BadRequest);
        Assert.Equal(unknownOwner, unknown.GetProperty("detail").GetString());

        var zero = await ExpectProblem(await admin.PostAsJsonAsync(Route("/api/v1/assets", lang), AssetBody(0, "X")), HttpStatusCode.BadRequest);
        Assert.Equal(invalidChoice, zero.GetProperty("errors").GetProperty("OwnerId")[0].GetString());
    }

    [Theory]
    [InlineData("nl-NL", "Object heeft nog kosten of activiteiten en kan niet worden verwijderd.")]
    [InlineData("en-US", "The asset still has expenses or activities and cannot be deleted.")]
    public async Task Delete_with_lines_message_follows_lang(string lang, string message)
    {
        var admin = await api.As("Admin");
        var assetId = await admin.CreateAsset(await admin.CreateOwner(api.Unique("B15 owner")), api.Unique("B15 house"));
        await admin.CreateExpense(assetId);

        var problem = await ExpectProblem(await admin.DeleteAsync(Route($"/api/v1/assets/{assetId}", lang)), HttpStatusCode.BadRequest);

        Assert.Equal(message, problem.GetProperty("detail").GetString());
    }
}
