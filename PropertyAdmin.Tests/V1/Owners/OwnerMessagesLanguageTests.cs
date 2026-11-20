using System.Net;
using System.Net.Http.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Owners;

/// <summary>
/// T-114, T-120 (REQ-011, REQ-002 US-2.1b; D-049): the owner messages come in the language of the query
/// parameter <c>lang</c> (nl-NL or en-US, case-insensitive), Dutch when it is missing or not supported.
/// Accept-Language is ignored.
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

    private static string WithLang(string path, string? lang) =>
        lang is null ? path : $"{path}?lang={Uri.EscapeDataString(lang)}";

    private static HttpRequestMessage Request(HttpMethod method, string path, string? lang, string? acceptLanguage)
    {
        var request = new HttpRequestMessage(method, WithLang(path, lang));
        if (acceptLanguage is not null) request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        return request;
    }

    [Theory]
    [InlineData("nl-NL", null, KvkNl, VatNl)]
    [InlineData("en-US", null, KvkEn, VatEn)]
    [InlineData("EN-us", null, KvkEn, VatEn)]
    [InlineData(null, null, KvkNl, VatNl)]
    [InlineData("de-DE", null, KvkNl, VatNl)]
    [InlineData(null, "en-US", KvkNl, VatNl)]
    public async Task Create_gives_the_kvk_and_btw_messages_in_the_language_of_lang(string? lang, string? acceptLanguage, string kvk, string vat)
    {
        var admin = await api.As("Admin");
        var request = Request(HttpMethod.Post, "/api/v1/owners", lang, acceptLanguage);
        request.Content = JsonContent.Create(Body(api.Unique("Language owner")));

        var errors = (await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(kvk, errors.GetProperty("KvkNumber")[0].GetString());
        Assert.Equal(vat, errors.GetProperty("VatNumber")[0].GetString());
    }

    [Theory]
    [InlineData("nl-NL", KvkNl, VatNl)]
    [InlineData("en-US", KvkEn, VatEn)]
    public async Task Update_gives_the_kvk_and_btw_messages_in_the_language_of_lang(string lang, string kvk, string vat)
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Language update owner"));
        var request = Request(HttpMethod.Put, $"/api/v1/owners/{id}", lang, null);
        request.Content = JsonContent.Create(Body(api.Unique("Language update owner")));

        var errors = (await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(kvk, errors.GetProperty("KvkNumber")[0].GetString());
        Assert.Equal(vat, errors.GetProperty("VatNumber")[0].GetString());
    }

    private const string RequiredNl = "Dit veld is verplicht.";
    private const string RequiredEn = "This field is required.";
    private const string EmailNl = "Dit is geen geldig e-mailadres.";
    private const string EmailEn = "This is not a valid email address.";
    private const string MaxLengthNl = "Dit veld mag maximaal 20 tekens bevatten.";
    private const string MaxLengthEn = "This field can contain at most 20 characters.";

    /// <summary>A body with an empty name, an invalid email and a house number of 21 characters; no KvK or BTW.</summary>
    private static object InvalidFieldsBody() => new
    {
        name = "", email = "no-email", phone = "0100000000", street = "Teststraat", houseNumber = new string('1', 21),
        postalCode = "1234 AB", city = "Teststad", country = "Nederland", kvkNumber = (string?)null, vatNumber = (string?)null,
    };

    /// <summary>T-127 (B18, D-049): the required, email-format and maximum-length messages follow lang on both routes.</summary>
    [Theory]
    [InlineData("POST", "nl-NL", RequiredNl, EmailNl, MaxLengthNl)]
    [InlineData("POST", "en-US", RequiredEn, EmailEn, MaxLengthEn)]
    [InlineData("POST", null, RequiredNl, EmailNl, MaxLengthNl)]
    [InlineData("PUT", "nl-NL", RequiredNl, EmailNl, MaxLengthNl)]
    [InlineData("PUT", "en-US", RequiredEn, EmailEn, MaxLengthEn)]
    public async Task Field_messages_come_in_the_language_of_lang(string method, string? lang, string required, string email, string maxLength)
    {
        var admin = await api.As("Admin");
        var path = "/api/v1/owners";
        if (method == "PUT") path += $"/{await admin.CreateOwner(api.Unique("Field language owner"))}";
        var request = Request(new HttpMethod(method), path, lang, null);
        request.Content = JsonContent.Create(InvalidFieldsBody());

        var errors = (await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest)).GetProperty("errors");

        Assert.Equal(required, errors.GetProperty("Name")[0].GetString());
        Assert.Equal(email, errors.GetProperty("Email")[0].GetString());
        Assert.Equal(maxLength, errors.GetProperty("HouseNumber")[0].GetString());
    }

    private const string HasAssetsNl = "Eigenaar heeft nog objecten en kan niet worden verwijderd.";
    private const string HasAssetsEn = "The owner still has assets and cannot be deleted.";

    /// <summary>T-120 (D-049): the 'owner still has assets' refusal in the language of lang, Dutch without it.</summary>
    [Theory]
    [InlineData("nl-NL", null, HasAssetsNl)]
    [InlineData("en-US", null, HasAssetsEn)]
    [InlineData("EN-us", null, HasAssetsEn)]
    [InlineData(null, null, HasAssetsNl)]
    [InlineData("de-DE", null, HasAssetsNl)]
    [InlineData(null, "en-US", HasAssetsNl)]
    public async Task Delete_of_an_owner_with_assets_gives_the_refusal_in_the_language_of_lang(string? lang, string? acceptLanguage, string expected)
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Language busy owner"));
        await admin.CreateAsset(id, api.Unique("Language busy house"));
        var request = Request(HttpMethod.Delete, $"/api/v1/owners/{id}", lang, acceptLanguage);

        var problem = await ExpectProblem(await admin.SendAsync(request), HttpStatusCode.BadRequest);

        Assert.Equal(expected, problem.GetProperty("detail").GetString());
    }
}
