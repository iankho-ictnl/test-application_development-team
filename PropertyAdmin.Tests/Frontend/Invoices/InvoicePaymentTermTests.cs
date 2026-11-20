extern alias Frontend;

using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using Frontend::test_application_for_development_team.Services;
using CreateModel = Frontend::test_application_for_development_team.Pages.Invoices.CreateModel;

namespace PropertyAdmin.Tests.Frontend.Invoices;

/// <summary>
/// REQ-007, BUG-6, D-057, T-160 (F10): the invoice create screen has no editable payment term. The page model sends
/// the fixed 14 days from one constant (no bound property, so a posted value cannot change it), and the screen shows
/// the term as read-only text in Dutch and English.
/// </summary>
public sealed class InvoicePaymentTermTests
{
    private const string TermKey = "Payment term: 14 days (due date = invoice date + 14 days)";

    [Fact]
    public void The_payment_term_is_a_constant_of_14_days_and_not_a_bindable_property()
    {
        Assert.Equal(14, CreateModel.PaymentTermDays);
        Assert.Null(typeof(CreateModel).GetProperty("PaymentTermDays", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
    }

    [Fact]
    public async Task Create_sends_the_fixed_14_days_to_the_api()
    {
        var api = new StubApi();
        var page = CreatePage(api);
        page.OwnerId = 7;
        page.Date = new DateOnly(2026, 3, 31);

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.NotNull(api.PostedBody);
        using var json = JsonDocument.Parse(api.PostedBody!);
        Assert.Equal(14, json.RootElement.GetProperty("paymentTermDays").GetInt32());
        Assert.Equal(7, json.RootElement.GetProperty("ownerId").GetInt32());
    }

    [Theory]
    [InlineData("nl-NL", "Betalingstermijn: 14 dagen (vervaldatum = factuurdatum + 14 dagen)")]
    [InlineData("en-US", "Payment term: 14 days (due date = invoice date + 14 days)")]
    public void The_read_only_payment_term_text_is_translated(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var text = RealLocalizer()[TermKey];

            Assert.False(text.ResourceNotFound);
            Assert.Equal(expected, text.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static IStringLocalizer<SharedResource> RealLocalizer() =>
        new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResource>>();

    private static CreateModel CreatePage(StubApi api)
    {
        var httpContext = new DefaultHttpContext();
        var client = new ApiClient(new HttpClient(api) { BaseAddress = new Uri("http://api.test/") });
        return new CreateModel(client, RealLocalizer())
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
            TempData = new TempDataDictionary(httpContext, new NoTempDataProvider()),
        };
    }

    /// <summary>Records the body of POST api/invoices and answers 201 with the created invoice.</summary>
    private sealed class StubApi : HttpMessageHandler
    {
        private const string Created = """{"id":5,"number":"2026-0001","ownerId":7,"ownerName":"O","date":"2026-03-31","dueDate":"2026-04-14","status":0,"paidDate":null,"totalExclVat":0,"totalVat":0,"totalInclVat":0,"lines":[],"vatSummary":[]}""";

        public string? PostedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") };
            PostedBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(Created, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
