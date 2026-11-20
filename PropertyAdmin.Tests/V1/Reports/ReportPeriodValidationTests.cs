using System.ComponentModel.DataAnnotations;
using System.Net;
using PropertyAdmin.Api.V1.Reports;
using PropertyAdmin.Tests.Characterization;
using static PropertyAdmin.Tests.Characterization.Http;
using AssetResults = PropertyAdmin.Api.V1.Reports.GetAssetResults;
using OwnerOverview = PropertyAdmin.Api.V1.Reports.GetOwnerOverview;

namespace PropertyAdmin.Tests.V1.Reports;

/// <summary>REQ-008, D-033 (T-161): the report period validation of both report requests, and that the
/// API returns it as 400 at the field in the language of lang.</summary>
[Collection(ApiCollection.Name)]
public sealed class ReportPeriodValidationTests(ApiFactory api)
{
    public static TheoryData<string?, string?, string, string> Invalid => new()
    {
        { null, "2025-12-31", "from", "This field is required." },
        { "2025-01-01", null, "to", "This field is required." },
        { "", "2025-12-31", "from", "This field is required." },
        { "abc", "2025-12-31", "from", "Enter a valid date as yyyy-MM-dd." },
        { "2025-01-01", "31-12-2025", "to", "Enter a valid date as yyyy-MM-dd." },
        { "2025-02-30", "2025-12-31", "from", "Enter a valid date as yyyy-MM-dd." },
        { "2025-12-31", "2025-01-01", "to", "The end date cannot be before the start date." },
    };

    [Theory]
    [MemberData(nameof(Invalid))]
    public void Invalid_period_gives_one_error_at_the_field(string? from, string? to, string field, string message)
    {
        AssertSingle(Validate(new AssetResults.Request(from, to)), field, message);
        AssertSingle(Validate(new OwnerOverview.Request(from, to)), field, message);
    }

    [Theory]
    [InlineData("2025-01-01", "2025-12-31")]
    [InlineData("2025-05-31", "2025-05-31")]
    public void Valid_period_has_no_errors_and_maps_to_the_dates(string from, string to)
    {
        Assert.Empty(Validate(new AssetResults.Request(from, to)));
        Assert.Empty(Validate(new OwnerOverview.Request(from, to)));
        var assetQuery = AssetResults.Mapping.ToQuery(new AssetResults.Request(from, to), 7);
        var ownerQuery = OwnerOverview.Mapping.ToQuery(new OwnerOverview.Request(from, to), null);
        Assert.Equal((DateOnly.Parse(from), DateOnly.Parse(to), (int?)7), (assetQuery.From, assetQuery.To, assetQuery.OwnerScopeId));
        Assert.Equal((DateOnly.Parse(from), DateOnly.Parse(to), (int?)null), (ownerQuery.From, ownerQuery.To, ownerQuery.OwnerScopeId));
    }

    [Fact]
    public void Both_fields_missing_gives_an_error_at_each()
    {
        var errors = Validate(new AssetResults.Request(null, null));
        Assert.Equal(["from", "to"], errors.SelectMany(error => error.MemberNames).ToArray());
    }

    [Theory]
    [InlineData("/api/v1/reports/asset-results")]
    [InlineData("/api/v1/reports/owner-overview")]
    [InlineData("/api/reports/asset-results")]
    [InlineData("/api/reports/owner-overview")]
    public async Task Api_returns_400_at_the_field_in_the_request_language(string route)
    {
        var viewer = await api.As("Viewer");

        var missing = (await ExpectProblem(await viewer.GetAsync($"{route}?to=2025-12-31&lang=en-US"), HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("This field is required.", missing.GetProperty("from")[0].GetString());

        var invalid = (await ExpectProblem(await viewer.GetAsync($"{route}?from=2025-01-01&to=abc&lang=nl-NL"), HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("Vul een geldige datum in als jjjj-mm-dd.", invalid.GetProperty("to")[0].GetString());

        var reversed = (await ExpectProblem(await viewer.GetAsync($"{route}?from=2025-12-31&to=2025-01-01"), HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.Equal("De einddatum mag niet voor de begindatum liggen.", reversed.GetProperty("to")[0].GetString());
        Assert.False(reversed.TryGetProperty("from", out _));

        await ExpectStatus(await viewer.GetAsync($"{route}?from=2025-01-01&to=2025-12-31&lang=en-US"), HttpStatusCode.OK);
    }

    private static List<ValidationResult> Validate(IValidatableObject request) =>
        request.Validate(new ValidationContext(request)).ToList();

    private static void AssertSingle(List<ValidationResult> errors, string field, string message)
    {
        var error = Assert.Single(errors);
        Assert.Equal(message, error.ErrorMessage);
        Assert.Equal([field], error.MemberNames.ToArray());
    }
}
