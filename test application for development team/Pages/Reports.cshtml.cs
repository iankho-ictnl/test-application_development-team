using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages;

[Authorize(Policy = Permissions.Reports)]
public class ReportsModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public const string ToBeforeFromMessage = "'To' must not be before 'From'";

    [BindProperty(SupportsGet = true), DateInput] public DateOnly From { get; set; } = new(DateTime.Today.Year, 1, 1);
    [BindProperty(SupportsGet = true), DateInput] public DateOnly To { get; set; } = new(DateTime.Today.Year, 12, 31);
    public List<AssetResultDto> Assets { get; set; } = [];
    public List<OwnerOverviewDto> Owners { get; set; } = [];

    /// <summary>D-033: the period is never swapped silently. A reversed period is a field error at <c>To</c> and calls no API;
    /// a 400 of the API shows at its field (<c>from</c>, <c>to</c>), any other error in the summary.</summary>
    public async Task OnGetAsync()
    {
        if (To < From) ModelState.AddModelError(nameof(To), L[ToBeforeFromMessage]);
        if (!ModelState.IsValid) return;

        var assets = await Api.TryGetAsync<List<AssetResultDto>>(Query.Build("api/reports/asset-results", ("from", From), ("to", To)));
        if (!assets.Ok) { AddApiError<ReportsModel>(assets, prefix: ""); return; }
        var owners = await Api.TryGetAsync<List<OwnerOverviewDto>>(Query.Build("api/reports/owner-overview", ("from", From), ("to", To)));
        if (!owners.Ok) { AddApiError<ReportsModel>(owners, prefix: ""); return; }
        Assets = assets.Value ?? [];
        Owners = owners.Value ?? [];
    }
}
