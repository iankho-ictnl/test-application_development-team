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
using PropertyAdmin.Contracts;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Pages;
using Frontend::test_application_for_development_team.Services;
using EditModel = Frontend::test_application_for_development_team.Pages.Assets.EditModel;

namespace PropertyAdmin.Tests.Frontend.Assets;

/// <summary>
/// REQ-003, D-041, T-066 (F6, closes BUG-5): the asset form sends the five address fields as the interface contract
/// says and shows the API's 400 at each field. The API is a test double (B15 is built at the same time).
/// </summary>
public sealed class AssetEditTests
{
    private static AssetInput ValidInput() => new()
    {
        OwnerId = 2, Type = AssetType.Home, Name = "Huis", Street = "Rue de la Loi", HouseNumber = "16A",
        PostalCode = "1000", City = "Brussel", Country = "België",
    };

    [Fact]
    public async Task Save_sends_the_five_address_fields_and_no_address()
    {
        var api = new StubApi(HttpStatusCode.Created, "{}");
        var page = CreatePage(api);
        page.Input = ValidInput();

        var result = await page.OnPostAsync(null);

        Assert.IsType<RedirectToPageResult>(result);
        var (method, path, body) = Assert.Single(api.Requests);
        Assert.Equal("POST", method);
        Assert.Equal("/api/assets", path);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("ownerId").GetInt32());
        Assert.Equal("Huis", root.GetProperty("name").GetString());
        Assert.Equal("Rue de la Loi", root.GetProperty("street").GetString());
        Assert.Equal("16A", root.GetProperty("houseNumber").GetString());
        Assert.Equal("1000", root.GetProperty("postalCode").GetString());
        Assert.Equal("Brussel", root.GetProperty("city").GetString());
        Assert.Equal("België", root.GetProperty("country").GetString());
        Assert.False(root.TryGetProperty("address", out _));
    }

    [Fact]
    public async Task Update_puts_to_the_asset()
    {
        var api = new StubApi(HttpStatusCode.NoContent, "");
        var page = CreatePage(api);
        page.Input = ValidInput();

        await page.OnPostAsync(7);

        var (method, path, _) = Assert.Single(api.Requests);
        Assert.Equal("PUT", method);
        Assert.Equal("/api/assets/7", path);
    }

    [Fact]
    public async Task A_400_from_the_api_is_shown_at_each_field()
    {
        const string problem = """{"title":"One or more validation errors occurred.","status":400,"errors":{"houseNumber":["Huisnummer is verplicht."],"Country":["Land is verplicht."],"$.street":["Straat is verplicht."]}}""";
        var page = CreatePage(new StubApi(HttpStatusCode.BadRequest, problem));
        page.Input = ValidInput();

        var result = await page.OnPostAsync(null);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Huisnummer is verplicht.", Assert.Single(page.ModelState["Input.HouseNumber"]!.Errors).ErrorMessage);
        Assert.Equal("Land is verplicht.", Assert.Single(page.ModelState["Input.Country"]!.Errors).ErrorMessage);
        Assert.Equal("Straat is verplicht.", Assert.Single(page.ModelState["Input.Street"]!.Errors).ErrorMessage);
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
    [InlineData(nameof(AssetInput.Name))]
    [InlineData(nameof(AssetInput.Street))]
    [InlineData(nameof(AssetInput.HouseNumber))]
    [InlineData(nameof(AssetInput.PostalCode))]
    [InlineData(nameof(AssetInput.City))]
    [InlineData(nameof(AssetInput.Country))]
    public void Each_required_field_left_empty_is_refused_at_that_field(string field)
    {
        var input = ValidInput();
        typeof(AssetInput).GetProperty(field)!.SetValue(input, "");

        var errors = Validate(input);

        Assert.Contains(errors, e => e.MemberNames.Contains(field));
    }

    [Fact]
    public void A_foreign_address_is_valid()
    {
        Assert.Empty(Validate(ValidInput()));
    }

    [Fact]
    public void From_maps_every_contract_field_into_the_form()
    {
        var dto = new AssetDto(5, 3, "Jansen", AssetType.Building, "Pand", "Damstraat", "12A", "1012 JS", "Amsterdam", "Nederland");

        var input = AssetInput.From(dto);

        Assert.Equal((3, AssetType.Building, "Pand", "Damstraat", "12A", "1012 JS", "Amsterdam", "Nederland"),
            (input.OwnerId, input.Type, input.Name, input.Street, input.HouseNumber, input.PostalCode, input.City, input.Country));
    }

    private static List<ValidationResult> Validate(AssetInput input)
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

    /// <summary>Stands in for the API: answers a save with one status and body (recorded), and the owner list
    /// the form reloads after a refused save with an empty list.</summary>
    private sealed class StubApi(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public List<(string Method, string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") };
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
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
