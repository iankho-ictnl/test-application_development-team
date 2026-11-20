extern alias Frontend;

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Services;
using ReportsModel = Frontend::test_application_for_development_team.Pages.ReportsModel;

namespace PropertyAdmin.Tests.Frontend.Reports;

/// <summary>
/// REQ-008, D-033, T-162 (F11): the report screen never swaps the period silently. "To" before "from" is a field error
/// at "to" and calls no API; a 400 of the API shows at its field. The API is a test double (B12 comes later).
/// </summary>
public sealed class ReportPeriodScreenTests
{
    private const string AssetRow = """[{"assetId":1,"assetName":"Huis","ownerName":"Jansen","income":100.00,"expenses":40.00,"result":60.00}]""";
    private const string OwnerRow = """[{"ownerId":3,"ownerName":"Jansen","assetCount":1,"income":100.00,"expenses":40.00,"openInvoiceCount":0,"openInvoiceAmount":0}]""";

    [Fact]
    public async Task A_reversed_period_is_a_field_error_at_to_and_calls_no_api()
    {
        var api = new StubApi();
        var page = CreatePage(api);
        page.From = new DateOnly(2026, 6, 1);
        page.To = new DateOnly(2026, 5, 31);

        await page.OnGetAsync();

        Assert.Empty(api.Requests);
        Assert.Equal(ReportsModel.ToBeforeFromMessage, Assert.Single(page.ModelState[nameof(ReportsModel.To)]!.Errors).ErrorMessage);
        Assert.Equal((new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 31)), (page.From, page.To));
        Assert.Empty(page.Assets);
        Assert.Empty(page.Owners);
    }

    [Fact]
    public async Task A_one_day_period_is_valid_and_sends_both_dates_to_both_reports()
    {
        var api = new StubApi();
        var page = CreatePage(api);
        page.From = page.To = new DateOnly(2026, 5, 31);

        await page.OnGetAsync();

        Assert.True(page.ModelState.IsValid);
        Assert.Equal(
            ["/api/reports/asset-results?from=2026-05-31&to=2026-05-31", "/api/reports/owner-overview?from=2026-05-31&to=2026-05-31"],
            api.Requests);
        Assert.Equal(60.00m, Assert.Single(page.Assets).Result);
        Assert.Equal(3, Assert.Single(page.Owners).OwnerId);
    }

    [Fact]
    public void The_default_period_is_the_current_calendar_year()
    {
        var page = CreatePage(new StubApi());
        var year = DateTime.Today.Year;

        Assert.Equal((new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), (page.From, page.To));
    }

    [Fact]
    public async Task A_binding_error_calls_no_api()
    {
        var api = new StubApi();
        var page = CreatePage(api);
        page.ModelState.AddModelError(nameof(ReportsModel.From), "The value 'x' is not valid.");

        await page.OnGetAsync();

        Assert.Empty(api.Requests);
    }

    [Fact]
    public async Task A_400_from_the_api_is_shown_at_its_field()
    {
        const string problem = """{"title":"One or more validation errors occurred.","status":400,"errors":{"from":["Van is verplicht."],"to":["Tot en met mag niet vóór Van liggen."]}}""";
        var api = new StubApi(HttpStatusCode.BadRequest, problem);
        var page = CreatePage(api);

        await page.OnGetAsync();

        Assert.Equal("Van is verplicht.", Assert.Single(page.ModelState[nameof(ReportsModel.From)]!.Errors).ErrorMessage);
        Assert.Equal("Tot en met mag niet vóór Van liggen.", Assert.Single(page.ModelState[nameof(ReportsModel.To)]!.Errors).ErrorMessage);
        Assert.False(page.ModelState.ContainsKey(""));
        Assert.Single(api.Requests);
        Assert.Empty(page.Assets);
    }

    [Fact]
    public async Task A_400_of_the_owner_overview_is_also_shown_at_its_field()
    {
        const string problem = """{"status":400,"errors":{"to":["To is required."]}}""";
        var api = new StubApi(HttpStatusCode.BadRequest, problem, failOnlyOwnerOverview: true);
        var page = CreatePage(api);

        await page.OnGetAsync();

        Assert.Equal("To is required.", Assert.Single(page.ModelState[nameof(ReportsModel.To)]!.Errors).ErrorMessage);
        Assert.Equal(2, api.Requests.Count);
    }

    [Fact]
    public async Task A_403_says_the_action_is_not_allowed()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.Forbidden, ""));

        await page.OnGetAsync();

        Assert.Equal("You do not have permission for this action.", Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Another_error_goes_to_the_summary_with_the_api_text()
    {
        var page = CreatePage(new StubApi(HttpStatusCode.InternalServerError, """{"title":"Er is iets misgegaan.","status":500}"""));

        await page.OnGetAsync();

        Assert.Equal("Er is iets misgegaan.", Assert.Single(page.ModelState[""]!.Errors).ErrorMessage);
    }

    [Theory]
    [InlineData("nl-NL", "'Tot en met' mag niet vóór 'Van' liggen")]
    [InlineData("en-US", "'To' must not be before 'From'")]
    public void The_reversed_period_message_is_in_the_users_language(string culture, string expected)
    {
        var localizer = new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResource>>();
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var text = localizer[ReportsModel.ToBeforeFromMessage];
            Assert.False(text.ResourceNotFound);
            Assert.Equal(expected, text.Value);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static ReportsModel CreatePage(StubApi api)
    {
        var httpContext = new DefaultHttpContext();
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("http://api.test/") });
        return new ReportsModel(client, new EchoLocalizer())
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };
    }

    /// <summary>Stands in for the API: records each request (path and query) and answers both reports with one row,
    /// or with the given failure (for every report, or only for the owner overview).</summary>
    private sealed class StubApi(HttpStatusCode failure = HttpStatusCode.OK, string json = "", bool failOnlyOwnerOverview = false) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add(request.RequestUri.PathAndQuery);
            var fails = failure != HttpStatusCode.OK && (!failOnlyOwnerOverview || path.EndsWith("owner-overview", StringComparison.Ordinal));
            if (fails)
            {
                var error = new HttpResponseMessage(failure);
                if (json.Length > 0) error.Content = new StringContent(json, Encoding.UTF8, "application/problem+json");
                return Task.FromResult(error);
            }
            var body = path.EndsWith("asset-results", StringComparison.Ordinal) ? AssetRow : OwnerRow;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    /// <summary>Returns the resource key itself, so the tests check which text the page chose.</summary>
    private sealed class EchoLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
