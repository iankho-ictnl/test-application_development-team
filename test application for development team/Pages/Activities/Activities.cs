using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Activities;

[Authorize(Policy = FrontendPolicies.Read)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<ActivityDto> Items { get; set; } = [];
    public List<SelectListItem> Assets { get; set; } = [];
    [BindProperty(SupportsGet = true)] public int? AssetId { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? From { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? To { get; set; }

    public async Task OnGetAsync()
    {
        Assets = await AssetOptionsAsync();
        Items = await Api.GetAsync<List<ActivityDto>>(Query.Build("api/activities", ("assetId", AssetId), ("from", From), ("to", To)));
    }
}

[Authorize(Policy = Permissions.ExpensesActivities)]
public class EditModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    [BindProperty] public ActivityInput Input { get; set; } = new();
    public int? Id { get; set; }
    public List<SelectListItem> Assets { get; set; } = [];
    public List<SelectListItem> VatRates { get; set; } = [];
    public List<SelectListItem> Kinds { get; set; } = [];
    public List<SelectListItem> PricingTypes { get; set; } = [];

    public async Task OnGetAsync(int? id, int? assetId)
    {
        Id = id;
        if (id is int i) Input = ActivityInput.From(await Api.GetAsync<ActivityDto>($"api/activities/{i}"));
        else if (assetId is int a) Input.AssetId = a;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        if (ModelState.IsValid)
        {
            var r = id is int i
                ? await Api.PutAsync($"api/activities/{i}", Input.ToRequest())
                : await Api.PostAsync("api/activities", Input.ToRequest());
            if (r.Ok)
            {
                Done("Activity saved.");
                return RedirectToPage("/Assets/Details", new { id = Input.AssetId });
            }
            AddApiError(r);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        Id = id;
        var r = await Api.DeleteAsync($"api/activities/{id}");
        if (r.Ok)
        {
            Done("Activity deleted.");
            return RedirectToPage("Index");
        }
        AddApiError(r);
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Assets = await AssetOptionsAsync();
        VatRates = await VatOptionsAsync();
        Kinds = EnumOptions<ActivityKind>();
        PricingTypes = EnumOptions<PricingType>();
    }
}
