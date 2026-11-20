using System.Net;
using System.Text.RegularExpressions;

namespace PropertyAdmin.Tests.Characterization.Screens;

/// <summary>What a browser does on the screens: get a page, post a form with its antiforgery token, follow nothing.</summary>
public static partial class Browser
{
    private static readonly Uri Origin = new("https://localhost");

    /// <summary>GET the page, expect 200 and return its text (HTML decoded, non-breaking spaces as spaces).</summary>
    public static async Task<string> Page(this HttpClient browser, string url)
    {
        var response = await browser.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {url}: expected 200, got {(int)response.StatusCode} {response.Headers.Location}");
        return Text(body);
    }

    /// <summary>
    /// GET the form page, then POST the hidden fields (antiforgery token, challenge, ...) of the form
    /// that posts to the target, with these fields on top, as a browser does. Only that form's
    /// fields: the layout's other forms (culture switch, sign-out) have their own hidden fields.
    /// </summary>
    public static async Task<HttpResponseMessage> Submit(this HttpClient browser, string pageUrl, Dictionary<string, string> fields, string? postUrl = null)
    {
        var page = await browser.GetAsync(pageUrl);
        var html = await page.Content.ReadAsStringAsync();
        Assert.True(page.StatusCode == HttpStatusCode.OK, $"GET {pageUrl}: expected 200, got {(int)page.StatusCode} {page.Headers.Location}");
        var target = postUrl ?? pageUrl;
        var formHtml = FormFor(html, pageUrl, target);
        Assert.True(formHtml is not null, $"GET {pageUrl}: no form that posts to {target}");
        var form = new Dictionary<string, string>();
        foreach (Match input in InputTag().Matches(formHtml))
        {
            var tag = input.Value;
            if (!tag.Contains("type=\"hidden\"", StringComparison.Ordinal)) continue;
            var name = Attribute(tag, "name");
            if (name is not null) form[name] = Attribute(tag, "value") ?? "";
        }
        Assert.True(form.ContainsKey("__RequestVerificationToken"), $"GET {pageUrl}: no antiforgery token in the page");
        foreach (var (name, value) in fields) form[name] = value;
        return await browser.PostAsync(postUrl ?? pageUrl, new FormUrlEncodedContent(form));
    }

    /// <summary>The post form whose action is the target: same URL first, then same path and handler, then same path.</summary>
    private static string? FormFor(string html, string pageUrl, string target)
    {
        static string PathOf(string url) => url.Split('?')[0].TrimEnd('/').ToLowerInvariant();
        static bool HasHandler(string url) => url.Contains("handler=", StringComparison.OrdinalIgnoreCase);
        string? best = null;
        var bestScore = 0;
        foreach (Match form in FormTag().Matches(html))
        {
            var tag = form.Groups[1].Value;
            if (!string.Equals(Attribute(tag, "method"), "post", StringComparison.OrdinalIgnoreCase)) continue;
            var action = Attribute(tag, "action");
            if (string.IsNullOrEmpty(action)) action = pageUrl;
            var score = string.Equals(action, target, StringComparison.OrdinalIgnoreCase) ? 3
                : PathOf(action) != PathOf(target) ? 0
                : HasHandler(action) == HasHandler(target) ? 2 : 1;
            if (score > bestScore) (best, bestScore) = (form.Value, score);
        }
        return best;
    }

    private static string? Attribute(string tag, string name)
    {
        var match = Regex.Match(tag, $"\\s{name}=\"([^\"]*)\"");
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    public static Uri ExpectRedirect(HttpResponseMessage response, string path)
    {
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Expected 302 to {path}, got {(int)response.StatusCode} {response.Headers.Location} {Errors(response)}");
        var location = response.Headers.Location!;
        if (!location.IsAbsoluteUri) location = new Uri(Origin, location);
        Assert.True(location.AbsolutePath == path, $"Expected 302 to {path}, got 302 to {location}");
        return location;
    }

    /// <summary>Expect a 302 whose path starts with the prefix (for a new id in the location); returns the absolute location.</summary>
    public static Uri ExpectRedirectPrefix(HttpResponseMessage response, string pathPrefix)
    {
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Expected 302 to {pathPrefix}..., got {(int)response.StatusCode} {response.Headers.Location} {Errors(response)}");
        var location = response.Headers.Location!;
        if (!location.IsAbsoluteUri) location = new Uri(Origin, location);
        Assert.True(location.AbsolutePath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase), $"Expected 302 to {pathPrefix}..., got 302 to {location}");
        return location;
    }

    /// <summary>The validation messages of a page that was shown again, for the failure message.</summary>
    private static string Errors(HttpResponseMessage response)
    {
        var html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return string.Join(" | ", ErrorText().Matches(html).Select(m => Text(Regex.Replace(m.Groups[1].Value, "<[^>]+>", " ")).Trim()).Where(t => t.Length > 0));
    }

    public static async Task ExpectSignInRedirect(this HttpClient browser, string url)
    {
        var location = ExpectRedirect(await browser.GetAsync(url), "/Account/Login");
        Assert.Contains("ReturnUrl=", location.Query);
    }

    public static string Text(string html) => WebUtility.HtmlDecode(html).Replace('\u00A0', ' ').Replace('\u202F', ' ');

    /// <summary>The validation messages at one field (its asp-validation-for span), for an invalid post.</summary>
    public static string FieldError(string html, string field)
    {
        var match = Regex.Match(html, $"<span[^>]*class=\"[^\"]*field-validation-error[^\"]*\"[^>]*data-valmsg-for=\"{Regex.Escape(field)}\"[^>]*>(.*?)</span>", RegexOptions.Singleline);
        if (!match.Success)
            match = Regex.Match(html, $"<span[^>]*data-valmsg-for=\"{Regex.Escape(field)}\"[^>]*class=\"[^\"]*field-validation-error[^\"]*\"[^>]*>(.*?)</span>", RegexOptions.Singleline);
        Assert.True(match.Success, $"No field-validation-error at {field}");
        var text = Text(Regex.Replace(match.Groups[1].Value, "<[^>]+>", " ")).Trim();
        Assert.False(string.IsNullOrEmpty(text), $"Empty field error at {field}");
        return text;
    }

    /// <summary>Post an invalid form: expect the form shown again (200) with an error at the field.</summary>
    public static async Task<string> ExpectFieldError(HttpResponseMessage response, string field)
    {
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 with a field error at {field}, got {(int)response.StatusCode} {response.Headers.Location}");
        return FieldError(html, field);
    }

    /// <summary>Post an invalid form on a screen without per-field messages: expect 200 with the error in the validation summary.</summary>
    public static async Task<string> ExpectSummaryError(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 with a validation summary, got {(int)response.StatusCode} {response.Headers.Location}");
        var match = Regex.Match(html, "class=\"[^\"]*validation-summary-errors[^\"]*\"[^>]*>(.*?)</div>", RegexOptions.Singleline);
        Assert.True(match.Success, "No validation-summary-errors in the page");
        var text = Text(Regex.Replace(match.Groups[1].Value, "<[^>]+>", " ")).Trim();
        Assert.False(string.IsNullOrEmpty(text), "Empty validation summary");
        return text;
    }

    [GeneratedRegex("<input\\b[^>]*>")]
    private static partial Regex InputTag();

    [GeneratedRegex("<form\\b([^>]*)>.*?</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FormTag();

    [GeneratedRegex("class=\"[^\"]*(?:validation-summary-errors|field-validation-error)[^\"]*\"[^>]*>(.*?)</(?:div|span)>", RegexOptions.Singleline)]
    private static partial Regex ErrorText();
}
