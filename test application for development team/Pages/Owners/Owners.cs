using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Owners;

[Authorize(Policy = FrontendPolicies.Read)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<OwnerDto> Owners { get; set; } = [];

    public async Task OnGetAsync() => Owners = await Api.GetAsync<List<OwnerDto>>("api/owners");

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var r = await Api.DeleteAsync($"api/owners/{id}");
        if (!r.Ok)
        {
            AddApiError(r);
            await OnGetAsync();
            return Page();
        }
        Done("Owner deleted.");
        return RedirectToPage();
    }
}

[Authorize(Policy = FrontendPolicies.Read)]
public class DetailsModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public OwnerDto Owner { get; set; } = null!;
    public List<AssetDto> Assets { get; set; } = [];
    public List<InvoiceDto> Invoices { get; set; } = [];

    public async Task OnGetAsync(int id)
    {
        Owner = await Api.GetAsync<OwnerDto>($"api/owners/{id}");
        Assets = await Api.GetAsync<List<AssetDto>>($"api/assets?ownerId={id}");
        Invoices = await Api.GetAsync<List<InvoiceDto>>($"api/invoices?ownerId={id}");
    }
}

[Authorize(Policy = Permissions.OwnersAssets)]
public class EditModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    [BindProperty] public OwnerInput Input { get; set; } = new();
    public int? Id { get; set; }

    public async Task OnGetAsync(int? id)
    {
        Id = id;
        if (id is int i) Input = OwnerInput.From(await Api.GetAsync<OwnerDto>($"api/owners/{i}"));
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        Id = id;
        if (!ModelState.IsValid) return Page();
        var r = id is int i
            ? await Api.PutAsync($"api/owners/{i}", Input.ToRequest())
            : await Api.PostAsync("api/owners", Input.ToRequest());
        if (!r.Ok)
        {
            AddApiError(r);
            return Page();
        }
        Done("Owner saved.");
        return RedirectToPage("Index");
    }
}
