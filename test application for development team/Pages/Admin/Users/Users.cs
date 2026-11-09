using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Admin.Users;

[Authorize(Policy = Permissions.Users)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<UserDto> Items { get; set; } = [];

    public async Task OnGetAsync() => Items = await Api.GetAsync<List<UserDto>>("api/users");
}

[Authorize(Policy = Permissions.Users)]
public class EditModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public string? Id { get; set; }
    public UserDto? Existing { get; set; }
    public List<SelectListItem> Owners { get; set; } = [];
    public List<string> AllRoles { get; set; } = [];

    [BindProperty, Required(ErrorMessage = "Required"), MaxLength(100), Display(Name = "User name")] public string UserName { get; set; } = "";
    [BindProperty, Required(ErrorMessage = "Required"), EmailAddress(ErrorMessage = "Invalid email address"), Display(Name = "Email")] public string Email { get; set; } = "";
    [BindProperty, DataType(DataType.Password), Display(Name = "Password")] public string? Password { get; set; }
    [BindProperty, Display(Name = "Linked owner")] public int? OwnerId { get; set; }
    [BindProperty] public List<string> Roles { get; set; } = [];
    [BindProperty, Display(Name = "Blocked")] public bool LockedOut { get; set; }

    public async Task OnGetAsync(string? id)
    {
        Id = id;
        if (id is not null)
        {
            Existing = await Api.GetAsync<UserDto>($"api/users/{Uri.EscapeDataString(id)}");
            (UserName, Email, OwnerId, Roles, LockedOut) = (Existing.UserName, Existing.Email, Existing.OwnerId, Existing.Roles.ToList(), Existing.LockedOut);
        }
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(string? id)
    {
        Id = id;
        if (id is null && string.IsNullOrEmpty(Password)) ModelState.AddModelError(nameof(Password), L["Required"]);
        if (ModelState.IsValid)
        {
            var r = id is null
                ? await Api.PostAsync("api/users", new UserCreateRequest(UserName, Email, Password!, OwnerId, Roles))
                : await Api.PutAsync($"api/users/{Uri.EscapeDataString(id)}", new UserUpdateRequest(Email, OwnerId, Roles, LockedOut));
            if (r.Ok)
            {
                Done("User saved.");
                return RedirectToPage("Index");
            }
            AddApiError(r);
        }
        await OnGetExistingAsync();
        return Page();
    }

    public Task<IActionResult> OnPostResetTwoFactorAsync(string id) => Action(id, "reset-2fa", "Two-factor sign-in reset.");
    public Task<IActionResult> OnPostSignOutAsync(string id) => Action(id, "sign-out", "User signed out everywhere.");

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        Id = id;
        var r = await Api.DeleteAsync($"api/users/{Uri.EscapeDataString(id)}");
        if (r.Ok)
        {
            Done("User deleted.");
            return RedirectToPage("Index");
        }
        AddApiError(r);
        await OnGetAsync(id);
        return Page();
    }

    private async Task<IActionResult> Action(string id, string action, string message)
    {
        var r = await Api.PostAsync($"api/users/{Uri.EscapeDataString(id)}/{action}", null);
        if (r.Ok) Done(message);
        else TempData["Error"] = r.Error;
        return RedirectToPage(new { id });
    }

    private async Task OnGetExistingAsync()
    {
        if (Id is not null) Existing = await Api.GetAsync<UserDto>($"api/users/{Uri.EscapeDataString(Id)}");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Owners = await OwnerOptionsAsync();
        AllRoles = (await Api.GetAsync<List<RoleDto>>("api/roles")).Select(r => r.Name).ToList();
    }
}
