extern alias Frontend;

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Pages;
using Frontend::test_application_for_development_team.Services;
using EditModel = Frontend::test_application_for_development_team.Pages.Owners.EditModel;

namespace PropertyAdmin.Tests.Frontend.Owners;

/// <summary>
/// REQ-002 (US-2.1, US-2.1a, US-2.1b, US-2.1d), D-041, T-102: the owner form sends the five address fields and
/// the optional KvK and BTW numbers as the interface contract says, and shows the API's 400 at each field.
/// The API is a test double (B13 is not built yet).
/// </summary>
public sealed class OwnerEditTests
{
    private static OwnerInput ValidInput() => new()
    {
        Name = "Jansen", Email = "jansen@example.nl", Phone = "0612345678", Street = "Rue de la Loi", HouseNumber = "16",
        PostalCode = "1000", City = "Brussel", Country = "België",
    };

    [Fact]
    public async Task Save_sends_the_contract_fields_and_null_for_empty_kvk_and_btw()
    {
        var api = new StubApi(HttpStatusCode.Created, "{}");
        var page = CreatePage(api);
        page.Input = ValidInput();
        page.Input.KvkNumber = " ";

        var result = await page.OnPostAsync(null);

        Assert.IsType<RedirectToPageResult>(result);
        var (method, path, body) = Assert.Single(api.Requests);
        Assert.Equal("POST", method);
        Assert.Equal("/api/owners", path);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal("Rue de la Loi", root.GetProperty("street").GetString());
        Assert.Equal("16", root.GetProperty("houseNumber").GetString());
        Assert.Equal("1000", root.GetProperty("postalCode").GetString());
        Assert.Equal("Brussel", root.GetProperty("city").GetString());
        Assert.Equal("België", root.GetProperty("country").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("kvkNumber").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("vatNumber").ValueKind);
        Assert.False(root.TryGetProperty("address", out _));
    }

    [Fact]
    public async Task Update_sends_kvk_and_btw_as_typed_the_api_normalizes_them()
    {
        var api = new StubApi(HttpStatusCode.NoContent, "");
        var page = CreatePage(api);
        page.Input = ValidInput();
        page.Input.KvkNumber = "12345678";
        page.Input.VatNumber = "DE 123.456.789";

        await page.OnPostAsync(7);

        var (method, path, body) = Assert.Single(api.Requests);
        Assert.Equal("PUT", method);
        Assert.Equal("/api/owners/7", path);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("12345678", doc.RootElement.GetProperty("kvkNumber").GetString());
        Assert.Equal("DE 123.456.789", doc.RootElement.GetProperty("vatNumber").GetString());
    }

    [Fact]
    public async Task A_400_from_the_api_is_shown_at_each_field()
    {
        const string problem = """{"title":"One or more validation errors occurred.","status":400,"errors":{"houseNumber":["Huisnummer is verplicht."],"VatNumber":["Ongeldig btw-nummer."],"$.kvkNumber":["Ongeldig KvK-nummer."]}}""";
        var page = CreatePage(new StubApi(HttpStatusCode.BadRequest, problem));
        page.Input = ValidInput();

        var result = await page.OnPostAsync(null);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Huisnummer is verplicht.", Assert.Single(page.ModelState["Input.HouseNumber"]!.Errors).ErrorMessage);
        Assert.Equal("Ongeldig btw-nummer.", Assert.Single(page.ModelState["Input.VatNumber"]!.Errors).ErrorMessage);
        Assert.Equal("Ongeldig KvK-nummer.", Assert.Single(page.ModelState["Input.KvkNumber"]!.Errors).ErrorMessage);
        Assert.False(page.ModelState.ContainsKey(""));
    }

    [Fact]
    public async Task A_400_without_field_errors_goes_to_the_summary()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.BadRequest, """{"title":"Bad request","status":400}"""));
        page.Input = ValidInput();

        await page.OnPostAsync(null);

        Assert.Equal("Bad request", Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task A_403_says_the_action_is_not_allowed()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.Forbidden, ""));
        page.Input = ValidInput();

        await page.OnPostAsync(null);

        Assert.Equal("You do not have permission for this action.", Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData(nameof(OwnerInput.Name))]
    [InlineData(nameof(OwnerInput.Email))]
    [InlineData(nameof(OwnerInput.Phone))]
    [InlineData(nameof(OwnerInput.Street))]
    [InlineData(nameof(OwnerInput.HouseNumber))]
    [InlineData(nameof(OwnerInput.PostalCode))]
    [InlineData(nameof(OwnerInput.City))]
    [InlineData(nameof(OwnerInput.Country))]
    public void Each_required_field_left_empty_is_refused_at_that_field(string field)
    {
        var input = ValidInput();
        typeof(OwnerInput).GetProperty(field)!.SetValue(input, "");

        var errors = Validate(input);

        Assert.Contains(errors, e => e.MemberNames.Contains(field));
    }

    [Fact]
    public void Without_kvk_and_btw_and_with_a_foreign_address_the_form_is_valid()
    {
        Assert.Empty(Validate(ValidInput()));
    }

    [Fact]
    public void From_maps_every_contract_field_into_the_form()
    {
        var dto = new PropertyAdmin.Contracts.OwnerDto(3, "Jansen", "j@example.nl", "06", "Damstraat", "12A", "1012 JS", "Amsterdam",
            "Nederland", "12345678", "NL123456789B01");

        var input = OwnerInput.From(dto);

        Assert.Equal(("Damstraat", "12A", "1012 JS", "Amsterdam", "Nederland", "12345678", "NL123456789B01"),
            (input.Street, input.HouseNumber, input.PostalCode, input.City, input.Country, input.KvkNumber, input.VatNumber));
    }

    private static List<ValidationResult> Validate(OwnerInput input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }

    private static EditModel CreatePage(StubApi api)
    {
        var httpContext = new DefaultHttpContext();
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("http://api.test/") });
        return new EditModel(client, new EchoLocalizer())
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(httpContext, new NoTempDataProvider()),
        };
    }

    /// <summary>Stands in for the API: records each request and answers with one status and body.</summary>
    private sealed class StubApi(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public List<(string Method, string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.Method.Method, request.RequestUri!.AbsolutePath, body));
            var response = new HttpResponseMessage(status);
            if (json.Length > 0) response.Content = new StringContent(json, Encoding.UTF8, "application/problem+json");
            return response;
        }
    }

    /// <summary>Returns the resource key itself, so the tests check which text the page chose.</summary>
    private sealed class EchoLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(System.Globalization.CultureInfo.CurrentCulture, name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
