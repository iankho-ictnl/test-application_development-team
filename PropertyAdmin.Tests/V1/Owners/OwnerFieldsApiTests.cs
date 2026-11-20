using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;

namespace PropertyAdmin.Tests.V1.Owners;

/// <summary>
/// T-104 (tester): REQ-002 owner fields through the real API (D-041). Expected results come from
/// US-2.1, US-2.1a, US-2.1b and US-2.1d, not from the code. Test data is fictitious.
/// TC-REQ2-1: every required field missing or blank gives 400 at that field (create and update; BUG-2).
/// TC-REQ2-2: KvK and BTW valid, invalid and empty (US-2.1b).
/// TC-REQ2-3: a foreign address is saved (US-2.1d).
/// TC-REQ2-4: the invoice PDF is still served after the owner changed (US-2.1c; content is checked by the
/// OwnerBlockLines and GetOwnerAsync unit tests in InvoiceServiceTests).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class OwnerFieldsApiTests(ApiFactory api)
{
    private const string Route = "/api/v1/owners";

    private static Dictionary<string, object?> ValidBody(string name) => new()
    {
        ["name"] = name,
        ["email"] = "eigenaar@example.test",
        ["phone"] = "0100000000",
        ["street"] = "Teststraat",
        ["houseNumber"] = "12A",
        ["postalCode"] = "1234 AB",
        ["city"] = "Teststad",
        ["country"] = "Nederland",
    };

    private static async Task ExpectErrorAt(HttpResponseMessage response, string field)
    {
        var json = await ExpectProblem(response, HttpStatusCode.BadRequest);
        var errors = json.GetProperty("errors").EnumerateObject().Select(e => e.Name.TrimStart('$', '.')).ToList();
        Assert.True(errors.Any(e => string.Equals(e, field, StringComparison.OrdinalIgnoreCase)),
            $"Expected an error at '{field}', got: {string.Join(", ", errors)}");
    }

    public static TheoryData<string, string?> RequiredFields => new()
    {
        { "name", null }, { "name", "" },
        { "email", null }, { "email", "" },
        { "phone", null }, { "phone", "" },
        { "street", null }, { "street", "" }, { "street", "   " },
        { "houseNumber", null }, { "houseNumber", "" },
        { "postalCode", null }, { "postalCode", "" },
        { "city", null }, { "city", "" },
        { "country", null }, { "country", "" },
    };

    [Theory] // TC-REQ2-1, US-2.1, US-2.1a, BUG-2
    [MemberData(nameof(RequiredFields))]
    public async Task Create_without_a_required_field_is_refused_at_that_field(string field, string? value)
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Required owner"));
        if (value is null) body.Remove(field); else body[field] = value;

        await ExpectErrorAt(await admin.PostAsJsonAsync(Route, body), field);
    }

    [Theory] // TC-REQ2-1, US-2.1, US-2.1a, US-2.2
    [MemberData(nameof(RequiredFields))]
    public async Task Update_without_a_required_field_is_refused_at_that_field(string field, string? value)
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Update owner"));
        var body = ValidBody(api.Unique("Update owner"));
        if (value is null) body.Remove(field); else body[field] = value;

        await ExpectErrorAt(await admin.PutAsJsonAsync($"{Route}/{id}", body), field);
    }

    [Fact] // TC-REQ2-1, BUG-2: the original reproduction, an owner with only a name
    public async Task Create_with_only_a_name_is_refused_on_both_routes()
    {
        var admin = await api.As("Admin");
        foreach (var route in new[] { "/api/owners", Route })
        {
            var response = await admin.PostAsJsonAsync(route, new { name = api.Unique("Only name") });
            var json = await ExpectProblem(response, HttpStatusCode.BadRequest);
            var errors = json.GetProperty("errors").EnumerateObject().Select(e => e.Name.ToLowerInvariant()).ToList();
            foreach (var field in new[] { "email", "phone", "street", "housenumber", "postalcode", "city", "country" })
                Assert.Contains(field, errors);
        }
    }

    [Theory] // TC-REQ2-2, US-2.1b: valid values are saved, BTW stored without spaces and dots, upper case (D-041)
    [InlineData("12345678", "DE123456789", "DE123456789")]
    [InlineData("12345678", "EL123456789", "EL123456789")]
    [InlineData("12345678", "BE0123456789", "BE0123456789")]
    [InlineData("12345678", "NL123456789B01", "NL123456789B01")]
    [InlineData("12345678", "nl 1234.56789.b01", "NL123456789B01")]
    [InlineData("87654321", "DE 123.456.789", "DE123456789")]
    public async Task Create_with_a_valid_kvk_and_btw_number_is_saved(string kvk, string vat, string storedVat)
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Kvk owner"));
        body["kvkNumber"] = kvk;
        body["vatNumber"] = vat;

        var created = await Expect(await admin.PostAsJsonAsync(Route, body), HttpStatusCode.Created);
        var read = await Expect(await admin.GetAsync($"{Route}/{created.GetProperty("id").GetInt32()}"), HttpStatusCode.OK);
        Assert.Equal(kvk, read.GetProperty("kvkNumber").GetString());
        Assert.Equal(storedVat, read.GetProperty("vatNumber").GetString());
    }

    [Theory] // TC-REQ2-2, US-2.1b: KvK and BTW are optional (private persons)
    [InlineData(null)]
    [InlineData("")]
    public async Task Create_without_kvk_and_btw_is_saved(string? value)
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Private owner"));
        if (value is not null) { body["kvkNumber"] = value; body["vatNumber"] = value; }

        var created = await Expect(await admin.PostAsJsonAsync(Route, body), HttpStatusCode.Created);
        var read = await Expect(await admin.GetAsync($"{Route}/{created.GetProperty("id").GetInt32()}"), HttpStatusCode.OK);
        Assert.True(IsNullOrEmpty(read.GetProperty("kvkNumber")));
        Assert.True(IsNullOrEmpty(read.GetProperty("vatNumber")));
    }

    [Theory] // TC-REQ2-2, US-2.1b: a KvK number is exactly 8 digits
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567A")]
    public async Task Create_with_an_invalid_kvk_number_is_refused_at_kvkNumber(string kvk)
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Bad kvk"));
        body["kvkNumber"] = kvk;

        await ExpectErrorAt(await admin.PostAsJsonAsync(Route, body), "kvkNumber");
    }

    [Theory] // TC-REQ2-2, US-2.1b: unknown prefix, Dutch format wrong, Greece as GR, too short or too long
    [InlineData("XX123456")]
    [InlineData("XX123")]
    [InlineData("NL123")]
    [InlineData("NL123456789")]
    [InlineData("GR123456789")]
    [InlineData("DE1")]
    [InlineData("DE1234567890123")]
    public async Task Create_with_an_invalid_btw_number_is_refused_at_vatNumber(string vat)
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Bad btw"));
        body["vatNumber"] = vat;

        await ExpectErrorAt(await admin.PostAsJsonAsync(Route, body), "vatNumber");
    }

    [Fact] // TC-REQ2-2, US-2.1b: the same rules on update
    public async Task Update_with_an_invalid_btw_or_kvk_number_is_refused_at_that_field()
    {
        var admin = await api.As("Admin");
        var id = await admin.CreateOwner(api.Unique("Update btw"));
        var body = ValidBody(api.Unique("Update btw"));
        body["vatNumber"] = "NL123";
        await ExpectErrorAt(await admin.PutAsJsonAsync($"{Route}/{id}", body), "vatNumber");

        body["vatNumber"] = null;
        body["kvkNumber"] = "123";
        await ExpectErrorAt(await admin.PutAsJsonAsync($"{Route}/{id}", body), "kvkNumber");
    }

    [Fact] // TC-REQ2-3, US-2.1d: a Belgian owner with postal code "1000" and a German VAT-less owner
    public async Task Create_with_a_foreign_address_is_saved_and_read_back()
    {
        var admin = await api.As("Admin");
        var body = ValidBody(api.Unique("Foreign owner"));
        body["street"] = "Rue de la Loi";
        body["houseNumber"] = "16";
        body["postalCode"] = "1000";
        body["city"] = "Brussel";
        body["country"] = "België";
        body["vatNumber"] = "BE0123456789";

        var created = await Expect(await admin.PostAsJsonAsync(Route, body), HttpStatusCode.Created);
        var read = await Expect(await admin.GetAsync($"{Route}/{created.GetProperty("id").GetInt32()}"), HttpStatusCode.OK);
        HasShape(read, OwnerShape);
        Assert.Equal("Rue de la Loi", read.GetProperty("street").GetString());
        Assert.Equal("16", read.GetProperty("houseNumber").GetString());
        Assert.Equal("1000", read.GetProperty("postalCode").GetString());
        Assert.Equal("Brussel", read.GetProperty("city").GetString());
        Assert.Equal("België", read.GetProperty("country").GetString());
    }

    [Fact] // TC-REQ2-4, US-2.1c, US-2.2: change the owner after invoicing; the update is stored and the old invoice's PDF still renders
    public async Task Owner_change_after_invoicing_is_stored_and_the_old_invoice_pdf_still_renders()
    {
        var admin = await api.As("Admin");
        var ownerId = await admin.CreateOwner(api.Unique("Pdf change owner"));
        await admin.CreateActivity(await admin.CreateAsset(ownerId, api.Unique("Pdf change house")));
        var invoice = await Expect(await admin.PostAsJsonAsync("/api/v1/invoices", new { ownerId, date = "2026-03-31", paymentTermDays = 14 }), HttpStatusCode.Created);
        var invoiceId = invoice.GetProperty("id").GetInt32();

        var body = ValidBody(api.Unique("Pdf change owner new"));
        body["street"] = "Nieuweweg";
        body["houseNumber"] = "7";
        body["kvkNumber"] = "87654321";
        body["vatNumber"] = "NL123456789B01";
        var update = await admin.PutAsJsonAsync($"{Route}/{ownerId}", body);
        Assert.True(update.IsSuccessStatusCode, $"Update gave {(int)update.StatusCode}");

        var read = await Expect(await admin.GetAsync($"{Route}/{ownerId}"), HttpStatusCode.OK);
        Assert.Equal("Nieuweweg", read.GetProperty("street").GetString());
        Assert.Equal("87654321", read.GetProperty("kvkNumber").GetString());

        var pdf = await admin.GetAsync($"/api/v1/invoices/{invoiceId}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..4]));
    }

    private static bool IsNullOrEmpty(JsonElement e) =>
        e.ValueKind == JsonValueKind.Null || (e.ValueKind == JsonValueKind.String && e.GetString() == "");
}
