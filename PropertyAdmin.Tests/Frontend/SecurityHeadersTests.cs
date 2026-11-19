extern alias Frontend;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using FrontendProgram = Frontend::Program;
using Frontend::test_application_for_development_team.Services;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// REQ-001, REQ-025, security finding S-25: every frontend response carries the content security
/// policy and the other browser security headers, and no page has inline script that the policy
/// would block.
/// </summary>
public sealed class SecurityHeadersTests : IClassFixture<SecurityHeadersTests.FrontendFactory>
{
    private readonly FrontendFactory _factory;

    public SecurityHeadersTests(FrontendFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/js/site.js")]
    [InlineData("/does-not-exist")]
    [InlineData("/")]
    public async Task Every_response_has_the_security_headers(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path);

        Assert.Equal(SecurityHeadersMiddleware.ContentSecurityPolicy, Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }

    [Fact]
    public void The_policy_allows_no_inline_script_and_no_framing()
    {
        var policy = SecurityHeadersMiddleware.ContentSecurityPolicy;

        Assert.Contains("default-src 'self'", policy);
        Assert.Contains("object-src 'none'", policy);
        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.Contains("form-action 'self'", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
        Assert.DoesNotContain("unsafe-eval", policy);
    }

    [Fact]
    public void No_page_has_inline_script_or_inline_event_handlers()
    {
        var pages = Path.Combine(FindFrontendFolder(), "Pages");
        var inline = new Regex(@"<script(?![^>]*\bsrc=)[^>]*>|\son[a-z]+\s*=|javascript:|\sstyle\s*=|<style", RegexOptions.IgnoreCase);

        var offenders = Directory.EnumerateFiles(pages, "*.cshtml", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file).Select((line, i) => (file, line, number: i + 1)))
            .Where(x => inline.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(pages, x.file)}:{x.number}: {x.line.Trim()}")
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Site_js_handles_the_data_attributes_the_pages_use()
    {
        var script = File.ReadAllText(Path.Combine(FindFrontendFolder(), "wwwroot", "js", "site.js"));

        Assert.Contains("data-confirm", script);
        Assert.Contains("data-autosubmit", script);
        Assert.Contains("data-pricing-toggle", script);
    }

    private static string FindFrontendFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "test application for development team");
            if (File.Exists(Path.Combine(candidate, "test application for development team.csproj")))
            {
                return candidate;
            }
        }
        throw new DirectoryNotFoundException("The frontend project folder was not found above the test output folder.");
    }

    public sealed class FrontendFactory : WebApplicationFactory<FrontendProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Api:BaseUrl", "http://api.test/");
        }
    }
}
