using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Owners;

/// <summary>
/// T-114 (REQ-011, REQ-002 US-2.1b; code review T-110 should-fix 2): the KvK and BTW messages come in the
/// request language from Accept-Language, Dutch when the header is missing or not supported.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class OwnerMessagesLanguageTests(ApiFactory api)
{
    private const string KvkNl = "KvK-nummer moet uit 8 cijfers bestaan.";
    private const string VatNl = "Btw-nummer heeft geen geldig EU-formaat.";
    private const string KvkEn = "The KvK number must consist of 8 digits.";
    private const string VatEn = "The VAT number is not in a valid EU format.";

    private static object Body(string name) => new
    {
        name, email = "eigenaar@example.test", phone = "0100000000", street = "Teststraat", houseNumber = "1",
        postalCode = "1234 AB", city = "Teststad", country = "Nederland", kvkNumber = "123", vatNumber = "XX123",
    };

    [Theory]
    [InlineData("nl-NL", KvkNl, VatNl)]
    [InlineData("en-US", KvkEn, VatEn)]
    [InlineData(null, KvkNl, VatNl)]
    [InlineData("fr-FR", KvkNl, VatNl)]
    public async Task Create_gives_the_kvk_and_btw_messages_in_the_request_language(string? language, string kvk, string vat)
    {
        var admin = await api.As("Admin");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/owners") { Content = JsonContent.Create(Body(api.Unique("Language owner"))) };
        if (language is not null) request.Headers.AcceptLanguage.ParseAdd(language);

        var errors = (await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(kvk, errors.GetProperty("KvkNumber")[0].GetString());
        Assert.Equal(vat, errors.GetProperty("VatNumber")[0].GetString());
    }

    [Theory]
    [InlineData("nl-NL", KvkNl, VatNl)]
    [InlineData("en-US", KvkEn, VatEn)]
    public async Task Update_gives_the_kvk_and_btw_messages_in_the_request_language(string language, string kvk, string vat)
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Language update owner"));
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/owners/{id}") { Content = JsonContent.Create(Body(api.Unique("Language update owner"))) };
        request.Headers.AcceptLanguage.ParseAdd(language);

        var errors = (await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(kvk, errors.GetProperty("KvkNumber")[0].GetString());
        Assert.Equal(vat, errors.GetProperty("VatNumber")[0].GetString());
    }

    private const string HasAssetsNl = "Eigenaar heeft nog objecten en kan niet worden verwijderd.";
    private const string HasAssetsEn = "The owner still has assets and cannot be deleted.";

    /// <summary>T-120 (D-049): the 'owner still has assets' refusal in the request language, Dutch without a header.</summary>
    [Theory]
    [InlineData("nl-NL", HasAssetsNl)]
    [InlineData("en-US", HasAssetsEn)]
    [InlineData(null, HasAssetsNl)]
    public async Task Delete_of_an_owner_with_assets_gives_the_refusal_in_the_request_language(string? language, string expected)
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Language busy owner"));
        await admin.CreateAsset(id, api.Unique("Language busy house"));
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/owners/{id}");
        if (language is not null) request.Headers.AcceptLanguage.ParseAdd(language);

        var problem = await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest);

        Assert.Equal(expected, problem.GetProperty("detail").GetString());
    }
}
