using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Assets;

[Authorize(Policy = FrontendPolicies.Read)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<AssetDto> Assets { get; set; } = [];
    public List<SelectListItem> Owners { get; set; } = [];
    [BindProperty(SupportsGet = true)] public int? OwnerId { get; set; }

    public async Task OnGetAsync()
    {
        Owners = await OwnerOptionsAsync();
        Assets = await Api.GetAsync<List<AssetDto>>(OwnerId is int o ? $"api/assets?ownerId={o}" : "api/assets");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var r = await Api.DeleteAsync($"api/assets/{id}");
        if (!r.Ok)
        {
            AddApiError(r);
            await OnGetAsync();
            return Page();
        }
        Done("Asset deleted.");
        return RedirectToPage();
    }
}

[Authorize(Policy = FrontendPolicies.Read)]
public class DetailsModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public AssetDto Asset { get; set; } = null!;
    public List<ExpenseDto> Expenses { get; set; } = [];
    public List<ActivityDto> Activities { get; set; } = [];

    public async Task OnGetAsync(int id)
    {
        Asset = await Api.GetAsync<AssetDto>($"api/assets/{id}");
        Expenses = await Api.GetAsync<List<ExpenseDto>>($"api/expenses?assetId={id}");
        Activities = await Api.GetAsync<List<ActivityDto>>($"api/activities?assetId={id}");
    }
}

[Authorize(Policy = Permissions.OwnersAssets)]
public class EditModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    [BindProperty] public AssetInput Input { get; set; } = new();
    public int? Id { get; set; }
    public List<SelectListItem> Owners { get; set; } = [];
    public List<SelectListItem> Types { get; set; } = [];

    public async Task OnGetAsync(int? id, int? ownerId)
    {
        Id = id;
        if (id is int i) Input = AssetInput.From(await Api.GetAsync<AssetDto>($"api/assets/{i}"));
        else if (ownerId is int o) Input.OwnerId = o;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        if (ModelState.IsValid)
        {
            var r = id is int i
                ? await Api.PutAsync($"api/assets/{i}", Input.ToRequest())
                : await Api.PostAsync("api/assets", Input.ToRequest());
            if (r.Ok)
            {
                Done("Asset saved.");
                return RedirectToPage("Index");
            }
            AddApiError(r);
        }
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Owners = await OwnerOptionsAsync();
        Types = EnumOptions<AssetType>();
    }
}
