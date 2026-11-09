using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Admin;

[Authorize(Policy = Permissions.Users)]
public class RolesModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<RoleDto> Items { get; set; } = [];

    [BindProperty] public string? Original { get; set; }
    [BindProperty, Required(ErrorMessage = "Required"), MaxLength(50), Display(Name = "Name")] public string Name { get; set; } = "";
    [BindProperty] public List<string> SelectedPermissions { get; set; } = [];

    public async Task OnGetAsync(string? edit)
    {
        await LoadAsync();
        if (edit is not null && Items.FirstOrDefault(r => r.Name == edit) is { } role)
        {
            Original = role.Name;
            Name = role.Name;
            SelectedPermissions = role.Permissions.ToList();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var request = new RoleSaveRequest(Name, SelectedPermissions);
            var r = Original is null
                ? await Api.PostAsync("api/roles", request)
                : await Api.PutAsync($"api/roles/{Uri.EscapeDataString(Original)}", request);
            if (r.Ok)
            {
                Done("Role saved.");
                return RedirectToPage();
            }
            AddApiError(r);
        }
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string name)
    {
        var r = await Api.DeleteAsync($"api/roles/{Uri.EscapeDataString(name)}");
        if (r.Ok)
        {
            Done("Role deleted.");
            return RedirectToPage();
        }
        AddApiError(r);
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync() => Items = await Api.GetAsync<List<RoleDto>>("api/roles");
}
