using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PropertyAdmin.Contracts;

namespace test_application_for_development_team.Services;

public class ApiUnauthorizedException : Exception;

public record ApiResult(bool Ok, string? Error, HttpStatusCode Status)
{
    public static ApiResult Success(HttpStatusCode s) => new(true, null, s);
}

public record ApiResult<T>(bool Ok, string? Error, HttpStatusCode Status, T? Value) : ApiResult(Ok, Error, Status);

/// <summary>Adds the signed-in user's API token to every outgoing request.</summary>
public class AccessTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = accessor.HttpContext?.User.FindFirst(AuthConstants.AccessTokenClaim)?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
            throw new ApiUnauthorizedException();
        return response;
    }
}

public class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public async Task<T> GetAsync<T>(string url)
    {
        var r = await http.GetAsync(url);
        if (r.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(url);
        if (r.StatusCode == HttpStatusCode.Forbidden) throw new UnauthorizedAccessException(url);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<(byte[] Bytes, string FileName)> GetFileAsync(string url)
    {
        var r = await http.GetAsync(url);
        if (r.StatusCode == HttpStatusCode.NotFound) throw new KeyNotFoundException(url);
        if (r.StatusCode == HttpStatusCode.Forbidden) throw new UnauthorizedAccessException(url);
        r.EnsureSuccessStatusCode();
        var name = r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName ?? "file";
        return (await r.Content.ReadAsByteArrayAsync(), name.Trim('"'));
    }

    public async Task<ApiResult<T>> PostAsync<T>(string url, object? body) => await Read<T>(await http.PostAsJsonAsync(url, body, Json));
    public async Task<ApiResult> PostAsync(string url, object? body) => await Read<object>(await http.PostAsJsonAsync(url, body, Json));
    public async Task<ApiResult<T>> PutAsync<T>(string url, object body) => await Read<T>(await http.PutAsJsonAsync(url, body, Json));
    public async Task<ApiResult> PutAsync(string url, object body) => await Read<object>(await http.PutAsJsonAsync(url, body, Json));
    public async Task<ApiResult> DeleteAsync(string url) => await Read<object>(await http.DeleteAsync(url));

    private static async Task<ApiResult<T>> Read<T>(HttpResponseMessage r)
    {
        if (r.IsSuccessStatusCode)
        {
            T? value = default;
            if (r.StatusCode != HttpStatusCode.NoContent && r.Content.Headers.ContentLength != 0 && typeof(T) != typeof(object))
                value = await r.Content.ReadFromJsonAsync<T>(Json);
            return new(true, null, r.StatusCode, value);
        }
        return new(false, await ErrorText(r), r.StatusCode, default);
    }

    private static async Task<string> ErrorText(HttpResponseMessage r)
    {
        switch (r.StatusCode)
        {
            case HttpStatusCode.Unauthorized: return "Unauthorized";
            case HttpStatusCode.Forbidden: return "Forbidden";
            case HttpStatusCode.NotFound: return "NotFound";
        }
        try
        {
            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var m)) return m.GetString() ?? "";
            if (root.TryGetProperty("errors", out var errors))
                return string.Join(" ", errors.EnumerateObject().SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString())));
            if (root.TryGetProperty("title", out var t)) return t.GetString() ?? "";
        }
        catch (JsonException)
        {
        }
        return r.ReasonPhrase ?? r.StatusCode.ToString();
    }
}
