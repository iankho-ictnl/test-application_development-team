using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
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

    /// <summary>The current authenticator code (RFC 6238: SHA-1, 30 seconds, 6 digits) for an ASP.NET Identity shared key.</summary>
    public static string AuthenticatorCode(string sharedKey)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var key = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in sharedKey.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                key.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }
        var counter = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        using var hmac = new HMACSHA1(key.ToArray());
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & int.MaxValue;
        return (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("<input\\b[^>]*>")]
    private static partial Regex InputTag();

    [GeneratedRegex("<form\\b([^>]*)>.*?</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FormTag();

    [GeneratedRegex("class=\"[^\"]*(?:validation-summary-errors|field-validation-error)[^\"]*\"[^>]*>(.*?)</(?:div|span)>", RegexOptions.Singleline)]
    private static partial Regex ErrorText();
}
