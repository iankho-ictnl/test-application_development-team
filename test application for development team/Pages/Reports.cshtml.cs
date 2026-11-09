using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages;

[Authorize(Policy = Permissions.Reports)]
public class ReportsModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    [BindProperty(SupportsGet = true)] public DateOnly From { get; set; } = new(DateTime.Today.Year, 1, 1);
    [BindProperty(SupportsGet = true)] public DateOnly To { get; set; } = new(DateTime.Today.Year, 12, 31);
    public List<AssetResultDto> Assets { get; set; } = [];
    public List<OwnerOverviewDto> Owners { get; set; } = [];

    public async Task OnGetAsync()
    {
        if (To < From) (From, To) = (To, From);
        Assets = await Api.GetAsync<List<AssetResultDto>>(Query.Build("api/reports/asset-results", ("from", From), ("to", To)));
        Owners = await Api.GetAsync<List<OwnerOverviewDto>>(Query.Build("api/reports/owner-overview", ("from", From), ("to", To)));
    }
}
