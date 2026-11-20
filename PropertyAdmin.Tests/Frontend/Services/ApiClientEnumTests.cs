extern alias Frontend;

using System.Net;
using System.Text;
using System.Text.Json;
using PropertyAdmin.Contracts;
using FrontendApiClient = Frontend::test_application_for_development_team.Services.ApiClient;

namespace PropertyAdmin.Tests.FrontendServices;

/// <summary>
/// BUG-7, D-043: the frontend ApiClient sends enums as numbers, the contract the API reads
/// (REQ-003 asset save, REQ-005 activity save).
/// </summary>
public class ApiClientEnumTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }

    private static FrontendApiClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });

    [Fact]
    public async Task Post_asset_sends_type_as_number()
    {
        var handler = new CapturingHandler();

        await Client(handler).PostAsync("api/assets", new AssetSaveRequest(1, AssetType.Home, "Huis", "Straat", "1", "1234 AB", "Utrecht", "Nederland"));

        using var doc = JsonDocument.Parse(handler.Body!);
        var type = doc.RootElement.GetProperty("type");
        Assert.Equal(JsonValueKind.Number, type.ValueKind);
        Assert.Equal((int)AssetType.Home, type.GetInt32());
    }

    [Fact]
    public async Task Put_activity_sends_kind_and_pricing_type_as_numbers()
    {
        var handler = new CapturingHandler();
        var request = new ActivitySaveRequest(1, new DateOnly(2026, 11, 20), ActivityKind.Maintenance, "Onderhoud",
            PricingType.Fixed, null, null, 100m, "Hoog");

        await Client(handler).PutAsync("api/activities/1", request);

        using var doc = JsonDocument.Parse(handler.Body!);
        Assert.Equal(JsonValueKind.Number, doc.RootElement.GetProperty("kind").ValueKind);
        Assert.Equal((int)ActivityKind.Maintenance, doc.RootElement.GetProperty("kind").GetInt32());
        Assert.Equal(JsonValueKind.Number, doc.RootElement.GetProperty("pricingType").ValueKind);
        Assert.Equal((int)PricingType.Fixed, doc.RootElement.GetProperty("pricingType").GetInt32());
    }

    [Fact]
    public async Task Get_reads_enums_sent_as_numbers()
    {
        const string json = "{\"id\":5,\"ownerId\":1,\"ownerName\":\"Jansen\",\"type\":1,\"name\":\"Huis\",\"address\":\"Straat 1\",\"postalCode\":\"1234 AB\",\"city\":\"Utrecht\"}";

        var asset = await Client(new StubHandler(json)).GetAsync<AssetDto>("api/assets/5");

        Assert.Equal(AssetType.Home, asset.Type);
    }
}
