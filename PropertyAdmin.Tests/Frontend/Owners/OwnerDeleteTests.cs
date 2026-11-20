extern alias Frontend;

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Services;
using IndexModel = Frontend::test_application_for_development_team.Pages.Owners.IndexModel;

namespace PropertyAdmin.Tests.Frontend.Owners;

/// <summary>
/// REQ-002 US-2.3, BUG-10, T-113, D-049 (T-121): deleting an owner that still has assets is refused with the reason the API
/// sends in its Problem Details <c>detail</c>, shown as received, in Dutch and in English. The API is a test double that
/// answers as the API's error handler does, in the language of the <c>lang</c> query parameter it receives.
/// </summary>
public sealed class OwnerDeleteTests
{
    private const string HasAssetsNl = "Eigenaar heeft nog objecten en kan niet worden verwijderd.";
    private const string HasAssetsEn = "The owner still has assets and cannot be deleted.";

    private static string OwnerHasAssets(string? lang) =>
        $$"""{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"Bad Request","status":400,"detail":"{{(lang == "en-US" ? HasAssetsEn : HasAssetsNl)}}","traceId":"00-abc-01"}""";

    [Theory]
    [InlineData("nl-NL", HasAssetsNl)]
    [InlineData("en-US", HasAssetsEn)]
    public async Task Delete_of_an_owner_with_assets_shows_why_it_is_refused(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var api = new StubApi(HttpStatusCode.BadRequest, OwnerHasAssets);
            var page = CreatePage(api, RealLocalizer());

            var result = await page.OnPostDeleteAsync(1);

            Assert.IsType<PageResult>(result);
            Assert.Equal(expected, Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
            Assert.Equal(culture, api.DeleteLanguage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task A_400_without_detail_still_shows_the_title()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.BadRequest, _ => """{"title":"Ongeldige aanvraag","status":400}"""), RealLocalizer());

        await page.OnPostDeleteAsync(1);

        Assert.Equal("Ongeldige aanvraag", Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Delete_of_an_owner_without_assets_returns_to_the_list()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.NoContent, _ => ""), RealLocalizer());

        var result = await page.OnPostDeleteAsync(3);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.True(page.ModelState.IsValid);
    }

    private static IStringLocalizer<SharedResource> RealLocalizer() =>
        new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResource>>();

    private static IndexModel CreatePage(StubApi api, IStringLocalizer<SharedResource> localizer)
    {
        var httpContext = new DefaultHttpContext();
        var handler = new LanguageQueryHandler { InnerHandler = api };
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });
        return new IndexModel(client, localizer)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(httpContext, new NoTempDataProvider()),
        };
    }

    /// <summary>Answers DELETE with the given status and the body for the request's <c>lang</c>, and GET api/owners (the list
    /// shown again) with an empty list.</summary>
    private sealed class StubApi(HttpStatusCode status, Func<string?, string> json) : HttpMessageHandler
    {
        public string? DeleteLanguage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Get)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });
            DeleteLanguage = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query)["lang"];
            var body = json(DeleteLanguage);
            var response = new HttpResponseMessage(status);
            if (body.Length > 0) response.Content = new StringContent(body, Encoding.UTF8, "application/problem+json");
            return Task.FromResult(response);
        }
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
