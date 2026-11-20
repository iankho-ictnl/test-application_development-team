extern alias Frontend;

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using FrontendProgram = Frontend::Program;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-001, security finding S-35 (D-044, D-023): the sign-in rate limit counts per real client address
/// through the forwarded-header chain of the whole frontend host. X-Forwarded-For is trusted only from the
/// known proxy (172.30.0.10, as in docker-compose.yml); a request from any other address is counted under
/// its own connection address, whatever X-Forwarded-For it sends.
/// </summary>
public sealed class SignInForwardedHeadersTests
{
    private const int Limit = 3;
    private static readonly IPAddress Proxy = IPAddress.Parse("172.30.0.10");
    private static readonly IPAddress Elsewhere = IPAddress.Parse("198.51.100.7");

    [Fact]
    public async Task From_the_known_proxy_each_forwarded_client_address_has_its_own_limit()
    {
        await using var factory = new FrontendFactory();
        var form = await GetLoginFormAsync(factory.Server);

        for (var i = 0; i < Limit; i++)
        {
            Assert.Equal(StatusCodes.Status200OK, await PostLoginAsync(factory.Server, form, Proxy, "203.0.113.10"));
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostLoginAsync(factory.Server, form, Proxy, "203.0.113.10"));
        Assert.Equal(StatusCodes.Status200OK, await PostLoginAsync(factory.Server, form, Proxy, "203.0.113.20"));
    }

    [Fact]
    public async Task From_another_address_the_forwarded_header_is_ignored_and_its_own_address_counts()
    {
        await using var factory = new FrontendFactory();
        var form = await GetLoginFormAsync(factory.Server);

        for (var i = 0; i < Limit; i++)
        {
            Assert.Equal(StatusCodes.Status200OK, await PostLoginAsync(factory.Server, form, Elsewhere, $"203.0.113.{i + 1}"));
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostLoginAsync(factory.Server, form, Elsewhere, "203.0.113.99"));
        // The forwarded addresses it sent were not charged: those clients still sign in through the proxy.
        Assert.Equal(StatusCodes.Status200OK, await PostLoginAsync(factory.Server, form, Proxy, "203.0.113.1"));
    }

    private static async Task<LoginForm> GetLoginFormAsync(TestServer server)
    {
        var context = await server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Get;
            c.Request.Path = "/Account/Login";
            c.Connection.RemoteIpAddress = Proxy;
        });
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var html = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "No antiforgery token on the login page.");
        var cookies = string.Join("; ", context.Response.Headers.SetCookie.Select(c => c!.Split(';')[0]));
        return new LoginForm(cookies, token.Groups[1].Value);
    }

    private static async Task<int> PostLoginAsync(TestServer server, LoginForm form, IPAddress connection, string forwardedFor)
    {
        var body = Encoding.UTF8.GetBytes(
            $"UserName=user&Password=wrong&__RequestVerificationToken={Uri.EscapeDataString(form.Token)}");
        var context = await server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Post;
            c.Request.Path = "/Account/Login";
            c.Request.ContentType = "application/x-www-form-urlencoded";
            c.Request.ContentLength = body.Length;
            c.Request.Body = new MemoryStream(body);
            c.Request.Headers.Cookie = form.Cookies;
            c.Request.Headers["X-Forwarded-For"] = forwardedFor;
            c.Connection.RemoteIpAddress = connection;
        });
        return context.Response.StatusCode;
    }

    private sealed record LoginForm(string Cookies, string Token);

    private sealed class FrontendFactory : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Api:BaseUrl", "http://api.test/");
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "172.30.0.10");
            builder.UseSetting("SignInRateLimit:PermitLimit", Limit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<ApiClient>().ConfigurePrimaryHttpMessageHandler(() => new RefusingApi()));
        }
    }

    /// <summary>Stands in for the API: every sign-in attempt is refused, so the page shows again with 200.</summary>
    private sealed class RefusingApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }
}
