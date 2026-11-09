using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages;

public abstract class ApiPageModel(ApiClient api, IStringLocalizer<SharedResource> localizer) : PageModel
{
    protected ApiClient Api { get; } = api;
    protected IStringLocalizer<SharedResource> L { get; } = localizer;

    protected void AddApiError(ApiResult r) =>
        ModelState.AddModelError("", r.Status == System.Net.HttpStatusCode.Forbidden ? L["You do not have permission for this action."] : r.Error ?? L["Something went wrong."]);

    protected void Done(string message) => TempData["Message"] = L[message].Value;

    protected async Task<List<SelectListItem>> OwnerOptionsAsync() =>
        (await Api.GetAsync<List<OwnerDto>>("api/owners")).Select(o => new SelectListItem(o.Name, o.Id.ToString())).ToList();

    protected async Task<List<SelectListItem>> AssetOptionsAsync() =>
        (await Api.GetAsync<List<AssetDto>>("api/assets")).Select(a => new SelectListItem($"{a.Name} ({a.OwnerName})", a.Id.ToString())).ToList();

    protected async Task<List<SelectListItem>> VatOptionsAsync() =>
        (await Api.GetAsync<List<VatRateDto>>("api/vatrates")).Select(v => v.Name).Distinct().Select(n => new SelectListItem(n, n)).ToList();

    protected List<SelectListItem> EnumOptions<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Select(v => new SelectListItem(L[v.ToString()], v.ToString())).ToList();
}

public class OwnerInput
{
    [Required(ErrorMessage = "Required"), MaxLength(200), Display(Name = "Name")] public string Name { get; set; } = "";
    [EmailAddress(ErrorMessage = "Invalid email address"), MaxLength(200), Display(Name = "Email")] public string? Email { get; set; }
    [MaxLength(50), Display(Name = "Phone")] public string? Phone { get; set; }
    [MaxLength(200), Display(Name = "Address")] public string? Address { get; set; }
    [MaxLength(20), Display(Name = "Postal code")] public string? PostalCode { get; set; }
    [MaxLength(100), Display(Name = "City")] public string? City { get; set; }

    public OwnerSaveRequest ToRequest() => new(Name, Email, Phone, Address, PostalCode, City);
    public static OwnerInput From(OwnerDto o) => new() { Name = o.Name, Email = o.Email, Phone = o.Phone, Address = o.Address, PostalCode = o.PostalCode, City = o.City };
}

public class AssetInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Owner")] public int OwnerId { get; set; }
    [Display(Name = "Type")] public AssetType Type { get; set; }
    [Required(ErrorMessage = "Required"), MaxLength(200), Display(Name = "Name")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(200), Display(Name = "Address")] public string Address { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(20), Display(Name = "Postal code")] public string PostalCode { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(100), Display(Name = "City")] public string City { get; set; } = "";

    public AssetSaveRequest ToRequest() => new(OwnerId, Type, Name, Address, PostalCode, City);
    public static AssetInput From(AssetDto a) => new() { OwnerId = a.OwnerId, Type = a.Type, Name = a.Name, Address = a.Address, PostalCode = a.PostalCode, City = a.City };
}

public class ExpenseInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Asset")] public int AssetId { get; set; }
    [Display(Name = "Date")] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "Required"), MaxLength(500), Display(Name = "Description")] public string Description { get; set; } = "";
    [MaxLength(100), Display(Name = "Category")] public string? Category { get; set; }
    [Display(Name = "Amount excl. VAT")] public decimal AmountExclVat { get; set; }
    [Required(ErrorMessage = "Required"), Display(Name = "VAT rate")] public string VatRateName { get; set; } = "";
    [Display(Name = "Re-invoice to owner")] public bool Reinvoiceable { get; set; }

    public ExpenseSaveRequest ToRequest() => new(AssetId, Date, Description, Category, AmountExclVat, VatRateName, Reinvoiceable);
    public static ExpenseInput From(ExpenseDto e) => new() { AssetId = e.AssetId, Date = e.Date, Description = e.Description, Category = e.Category, AmountExclVat = e.AmountExclVat, VatRateName = e.VatRateName, Reinvoiceable = e.Reinvoiceable };
}

public class ActivityInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Asset")] public int AssetId { get; set; }
    [Display(Name = "Date")] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Kind")] public ActivityKind Kind { get; set; }
    [Required(ErrorMessage = "Required"), MaxLength(500), Display(Name = "Description")] public string Description { get; set; } = "";
    [Display(Name = "Pricing")] public PricingType PricingType { get; set; }
    [Display(Name = "Hours")] public decimal? Hours { get; set; }
    [Display(Name = "Hourly rate")] public decimal? HourlyRate { get; set; }
    [Display(Name = "Fixed price")] public decimal? FixedPrice { get; set; }
    [Required(ErrorMessage = "Required"), Display(Name = "VAT rate")] public string VatRateName { get; set; } = "";

    public ActivitySaveRequest ToRequest() => new(AssetId, Date, Kind, Description, PricingType, Hours, HourlyRate, FixedPrice, VatRateName);
    public static ActivityInput From(ActivityDto a) => new() { AssetId = a.AssetId, Date = a.Date, Kind = a.Kind, Description = a.Description, PricingType = a.PricingType, Hours = a.Hours, HourlyRate = a.HourlyRate, FixedPrice = a.FixedPrice, VatRateName = a.VatRateName };
}

public class VatRateInput
{
    [Required(ErrorMessage = "Required"), MaxLength(50), Display(Name = "Name")] public string Name { get; set; } = "";
    [Range(0, 100, ErrorMessage = "Between 0 and 100"), Display(Name = "Percentage")] public decimal Percentage { get; set; }
    [Display(Name = "Valid from")] public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Valid to")] public DateOnly? ValidTo { get; set; }

    public VatRateSaveRequest ToRequest() => new(Name, Percentage, ValidFrom, ValidTo);
    public static VatRateInput From(VatRateDto v) => new() { Name = v.Name, Percentage = v.Percentage, ValidFrom = v.ValidFrom, ValidTo = v.ValidTo };
}
