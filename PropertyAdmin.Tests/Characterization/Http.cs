using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PropertyAdmin.Tests.Characterization;

/// <summary>Assertions and data helpers shared by the characterization tests.</summary>
public static class Http
{
    public static async Task<JsonElement> Expect(HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");
        return body.Length == 0 ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    public static async Task ExpectStatus(HttpResponseMessage response, HttpStatusCode status) => await Expect(response, status);

    /// <summary>The body is Problem Details with this status (and, for domain errors, this title).</summary>
    public static async Task<JsonElement> ExpectProblem(HttpResponseMessage response, HttpStatusCode status, string? title = null)
    {
        var json = await Expect(response, status);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)status, json.GetProperty("status").GetInt32());
        if (title is not null) Assert.Equal(title, json.GetProperty("title").GetString());
        return json;
    }

    public static async Task ExpectValidationProblem(HttpResponseMessage response)
    {
        var json = await ExpectProblem(response, HttpStatusCode.BadRequest);
        Assert.Equal(JsonValueKind.Object, json.GetProperty("errors").ValueKind);
    }

    public const string DomainTitle = "Ongeldige aanvraag";

    /// <summary>The object has exactly these properties (camelCase JSON), which fixes the response shape.</summary>
    public static void HasShape(JsonElement e, params string[] names)
    {
        Assert.Equal(JsonValueKind.Object, e.ValueKind);
        Assert.Equal(names.OrderBy(x => x), e.EnumerateObject().Select(p => p.Name).OrderBy(x => x));
    }

    public static IEnumerable<int> Ids(JsonElement array) => array.EnumerateArray().Select(x => x.GetProperty("id").GetInt32());

    public static readonly string[] OwnerShape = ["id", "name", "email", "phone", "address", "postalCode", "city"];
    public static readonly string[] AssetShape = ["id", "ownerId", "ownerName", "type", "name", "address", "postalCode", "city"];
    public static readonly string[] VatRateShape = ["id", "name", "percentage", "validFrom", "validTo"];
    public static readonly string[] ExpenseShape = ["id", "assetId", "assetName", "date", "description", "category", "amountExclVat", "vatRateName", "vatPercentage", "vatAmount", "amountInclVat", "reinvoiceable", "invoiceId"];
    public static readonly string[] ActivityShape = ["id", "assetId", "assetName", "date", "kind", "description", "pricingType", "hours", "hourlyRate", "fixedPrice", "amountExclVat", "vatRateName", "vatPercentage", "vatAmount", "amountInclVat", "invoiceId"];
    public static readonly string[] InvoiceShape = ["id", "number", "ownerId", "ownerName", "date", "dueDate", "status", "paidDate", "totalExclVat", "totalVat", "totalInclVat", "lines", "vatSummary"];
    public static readonly string[] UserShape = ["id", "userName", "email", "twoFactorEnabled", "lockedOut", "ownerId", "roles"];

    public static async Task<int> CreateOwner(this HttpClient admin, string name)
    {
        var json = await Expect(await admin.PostAsJsonAsync("/api/owners", new { name, email = "eigenaar@example.test", phone = "0100000000", address = "Teststraat 1", postalCode = "1234 AB", city = "Teststad" }), HttpStatusCode.Created);
        return json.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateAsset(this HttpClient admin, int ownerId, string name)
    {
        var json = await Expect(await admin.PostAsJsonAsync("/api/assets", new { ownerId, type = 0, name, address = "Objectweg 2", postalCode = "1234 CD", city = "Teststad" }), HttpStatusCode.Created);
        return json.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateExpense(this HttpClient admin, int assetId, bool reinvoiceable = true, string date = "2026-03-01")
    {
        var json = await Expect(await admin.PostAsJsonAsync("/api/expenses", new { assetId, date, description = "Lamp vervangen", category = "Onderhoud", amountExclVat = 100.00m, vatRateName = "Hoog", reinvoiceable }), HttpStatusCode.Created);
        return json.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateActivity(this HttpClient admin, int assetId, string date = "2026-03-02")
    {
        var json = await Expect(await admin.PostAsJsonAsync("/api/activities", new { assetId, date, kind = 1, description = "Inspectie dak", pricingType = 1, fixedPrice = 200.00m, vatRateName = "Hoog" }), HttpStatusCode.Created);
        return json.GetProperty("id").GetInt32();
    }
}
