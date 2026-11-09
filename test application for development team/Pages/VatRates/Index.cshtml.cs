using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.VatRates;

[Authorize(Policy = Permissions.VatRates)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<VatRateDto> Items { get; set; } = [];
    [BindProperty] public VatRateInput Input { get; set; } = new();
    [BindProperty] public int? EditId { get; set; }

    public async Task OnGetAsync(int? edit)
    {
        await LoadAsync();
        if (edit is int id && Items.FirstOrDefault(v => v.Id == id) is { } v)
        {
            EditId = id;
            Input = VatRateInput.From(v);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var r = EditId is int id
                ? await Api.PutAsync($"api/vatrates/{id}", Input.ToRequest())
                : await Api.PostAsync("api/vatrates", Input.ToRequest());
            if (r.Ok)
            {
                Done("VAT rate saved.");
                return RedirectToPage();
            }
            AddApiError(r);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var r = await Api.DeleteAsync($"api/vatrates/{id}");
        if (r.Ok)
        {
            Done("VAT rate deleted.");
            return RedirectToPage();
        }
        AddApiError(r);
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync() =>
        Items = (await Api.GetAsync<List<VatRateDto>>("api/vatrates")).OrderBy(v => v.Name).ThenByDescending(v => v.ValidFrom).ToList();
}
