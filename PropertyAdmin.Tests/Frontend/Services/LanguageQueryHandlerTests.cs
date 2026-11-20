extern alias Frontend;

using System.Globalization;
using System.Net;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend.Services;

/// <summary>
/// REQ-002, REQ-011, D-049 (T-121, F8): every API call carries <c>lang=&lt;the user's screen language&gt;</c> in its query,
/// appended to any existing query; no Accept-Language header.
/// </summary>
public sealed class LanguageQueryHandlerTests
{
    [Theory]
    [InlineData("nl-NL", "api/owners/5", "http://api.test/api/owners/5?lang=nl-NL")]
    [InlineData("en-US", "api/owners/5", "http://api.test/api/owners/5?lang=en-US")]
    [InlineData("nl-NL", "api/reports/asset?from=2026-01-01&to=2026-12-31", "http://api.test/api/reports/asset?from=2026-01-01&to=2026-12-31&lang=nl-NL")]
    [InlineData("en-US", "api/reports/asset?from=2026-01-01&to=2026-12-31", "http://api.test/api/reports/asset?from=2026-01-01&to=2026-12-31&lang=en-US")]
    public async Task Every_request_gets_the_screen_language_in_its_query(string culture, string url, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            var recorder = new Recorder();
            using var client = new HttpClient(new LanguageQueryHandler { InnerHandler = recorder }) { BaseAddress = new Uri("http://api.test/") };

            await client.DeleteAsync(url);

            Assert.Equal(expected, recorder.Request!.RequestUri!.AbsoluteUri);
            Assert.Empty(recorder.Request.Headers.AcceptLanguage);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public async Task The_body_and_method_are_kept()
    {
        var recorder = new Recorder();
        using var client = new HttpClient(new LanguageQueryHandler { InnerHandler = recorder }) { BaseAddress = new Uri("http://api.test/") };

        await client.PostAsync("api/owners", new StringContent("{}"));

        Assert.Equal(HttpMethod.Post, recorder.Request!.Method);
        Assert.Equal("{}", recorder.Body);
    }

    [Theory]
    [InlineData("api/x", "api/x?lang=en-US")]
    [InlineData("api/x?", "api/x?lang=en-US")]
    [InlineData("api/x?a=1&", "api/x?a=1&lang=en-US")]
    [InlineData("api/x?a=1#top", "api/x?a=1&lang=en-US#top")]
    public void WithLanguage_appends_with_the_right_separator(string url, string expected) =>
        Assert.Equal(expected, LanguageQueryHandler.WithLanguage(new Uri(url, UriKind.Relative), "en-US").OriginalString);

    [Fact]
    public void WithLanguage_url_encodes_the_value() =>
        Assert.Equal("api/x?lang=a%26b%3Dc", LanguageQueryHandler.WithLanguage(new Uri("api/x", UriKind.Relative), "a&b=c").OriginalString);

    private sealed class Recorder : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
