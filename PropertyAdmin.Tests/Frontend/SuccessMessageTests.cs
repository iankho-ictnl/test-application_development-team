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
using OwnerEditModel = Frontend::test_application_for_development_team.Pages.Owners.EditModel;
using OwnerIndexModel = Frontend::test_application_for_development_team.Pages.Owners.IndexModel;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-011, BUG-11, T-119: every success message after a save, delete or action is shown in the user's language
/// (Dutch by default, English), through the resource files.
/// </summary>
public sealed class SuccessMessageTests
{
    /// <summary>Every text the page models pass to <c>Done</c> (the success message), with its Dutch translation.</summary>
    public static TheoryData<string, string> Messages => new()
    {
        { "Activity deleted.", "Activiteit verwijderd." },
        { "Activity saved.", "Activiteit opgeslagen." },
        { "Asset deleted.", "Object verwijderd." },
        { "Asset saved.", "Object opgeslagen." },
        { "Expense deleted.", "Uitgave verwijderd." },
        { "Expense saved.", "Uitgave opgeslagen." },
        { "Invoice created.", "Factuur aangemaakt." },
        { "Invoice deleted.", "Factuur verwijderd." },
        { "Invoice marked as paid.", "Factuur gemarkeerd als betaald." },
        { "Owner deleted.", "Eigenaar verwijderd." },
        { "Owner saved.", "Eigenaar opgeslagen." },
        { "Role deleted.", "Rol verwijderd." },
        { "Role saved.", "Rol opgeslagen." },
        { "Two-factor sign-in reset.", "Tweestapsaanmelding gereset." },
        { "User deleted.", "Gebruiker verwijderd." },
        { "User saved.", "Gebruiker opgeslagen." },
        { "User signed out everywhere.", "Gebruiker overal afgemeld." },
        { "VAT rate deleted.", "Btw-tarief verwijderd." },
        { "VAT rate saved.", "Btw-tarief opgeslagen." },
    };

    [Theory]
    [MemberData(nameof(Messages))]
    public void Every_success_message_has_a_Dutch_and_an_English_text(string key, string dutch)
    {
        Assert.Equal(dutch, Translate(key, "nl-NL"));
        Assert.Equal(key, Translate(key, "en-US"));
    }

    [Theory]
    [InlineData("nl-NL", "Eigenaar verwijderd.")]
    [InlineData("en-US", "Owner deleted.")]
    public async Task Deleting_an_owner_shows_the_success_message_in_the_users_language(string culture, string expected)
    {
        var page = Prepare(new OwnerIndexModel(Client(), RealLocalizer()));

        await InCulture(culture, () => page.OnPostDeleteAsync(3));

        Assert.Equal(expected, page.TempData["Message"]);
    }

    [Theory]
    [InlineData("nl-NL", "Eigenaar opgeslagen.")]
    [InlineData("en-US", "Owner saved.")]
    public async Task Saving_an_owner_shows_the_success_message_in_the_users_language(string culture, string expected)
    {
        var page = Prepare(new OwnerEditModel(Client(), RealLocalizer()));
        page.Input = new Frontend::test_application_for_development_team.Pages.OwnerInput
        {
            Name = "Test BV", Email = "test@example.org", Phone = "0612345678", Street = "Teststraat", HouseNumber = "1",
            PostalCode = "1234 AB", City = "Teststad", Country = "Nederland",
        };

        await InCulture(culture, () => page.OnPostAsync(null));

        Assert.Equal(expected, page.TempData["Message"]);
    }

    private static string Translate(string key, string culture) => WithCulture(culture, () =>
    {
        var text = RealLocalizer()[key];
        Assert.False(text.ResourceNotFound, $"No {culture} resource entry for \"{key}\".");
        return text.Value;
    });

    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try { return action(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static async Task InCulture(string culture, Func<Task> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try { await action(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static IStringLocalizer<SharedResource> RealLocalizer() =>
        new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResource>>();

    private static ApiClient Client() => new(new HttpClient(new OkApi()) { BaseAddress = new Uri("http://api.test/") });

    private static T Prepare<T>(T page) where T : PageModel
    {
        var httpContext = new DefaultHttpContext();
        page.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        page.TempData = new TempDataDictionary(httpContext, new NoTempDataProvider());
        return page;
    }

    /// <summary>Answers every write with 200 and an owner, every read with an empty list, as a successful API does.</summary>
    private sealed class OkApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Delete) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            var json = request.Method == HttpMethod.Get ? "[]" : """{"id":3,"name":"Test BV"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class NoTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
