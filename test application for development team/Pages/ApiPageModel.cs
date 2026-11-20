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

    /// <summary>Puts the API's message in the summary. The API's business-rule messages are Dutch; a known one is translated
    /// through the resource files (keyed by the Dutch text), an unknown one is shown as the API sent it.</summary>
    protected void AddApiError(ApiResult r) =>
        ModelState.AddModelError("", r.Status == System.Net.HttpStatusCode.Forbidden ? L["You do not have permission for this action."]
            : string.IsNullOrEmpty(r.Error) ? L["Something went wrong."] : L[r.Error]);

    /// <summary>Puts the API's 400 errors at the matching field of <typeparamref name="TInput"/> (bound as <paramref name="prefix"/>); the rest goes to the summary.</summary>
    protected void AddApiError<TInput>(ApiResult r, string prefix = "Input")
    {
        var placed = false;
        foreach (var (key, messages) in r.FieldErrors)
        {
            var name = key.StartsWith("$.") ? key[2..] : key;
            var property = typeof(TInput).GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
            if (property is null) continue;
            foreach (var m in messages) ModelState.AddModelError($"{prefix}.{property.Name}", m);
            placed = true;
        }
        if (!placed) AddApiError(r);
    }

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
    [Required(ErrorMessage = "Required"), MaxLength(200, ErrorMessage = "Maximum {1} characters"), Display(Name = "Name")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Required"), EmailAddress(ErrorMessage = "Invalid email address"), MaxLength(200, ErrorMessage = "Maximum {1} characters"), Display(Name = "Email")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(50, ErrorMessage = "Maximum {1} characters"), Display(Name = "Phone")] public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(200, ErrorMessage = "Maximum {1} characters"), Display(Name = "Street")] public string Street { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(20, ErrorMessage = "Maximum {1} characters"), Display(Name = "House number")] public string HouseNumber { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(20, ErrorMessage = "Maximum {1} characters"), Display(Name = "Postal code")] public string PostalCode { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(100, ErrorMessage = "Maximum {1} characters"), Display(Name = "City")] public string City { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(100, ErrorMessage = "Maximum {1} characters"), Display(Name = "Country")] public string Country { get; set; } = "";
    // Optional; the format (8 digits, EU VAT number) is checked by the API only (D-041).
    [Display(Name = "KvK number (optional)")] public string? KvkNumber { get; set; }
    [Display(Name = "VAT number (optional)")] public string? VatNumber { get; set; }

    public OwnerSaveRequest ToRequest() => new(Name, Email, Phone, Street, HouseNumber, PostalCode, City, Country,
        string.IsNullOrWhiteSpace(KvkNumber) ? null : KvkNumber, string.IsNullOrWhiteSpace(VatNumber) ? null : VatNumber);
    public static OwnerInput From(OwnerDto o) => new()
    {
        Name = o.Name, Email = o.Email ?? "", Phone = o.Phone ?? "", Street = o.Street, HouseNumber = o.HouseNumber,
        PostalCode = o.PostalCode, City = o.City, Country = o.Country, KvkNumber = o.KvkNumber, VatNumber = o.VatNumber,
    };
}

public class AssetInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Owner")] public int OwnerId { get; set; }
    [Display(Name = "Type")] public AssetType Type { get; set; }
    [Required(ErrorMessage = "Required"), MaxLength(200, ErrorMessage = "Maximum {1} characters"), Display(Name = "Name")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(200, ErrorMessage = "Maximum {1} characters"), Display(Name = "Address")] public string Address { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(20, ErrorMessage = "Maximum {1} characters"), Display(Name = "Postal code")] public string PostalCode { get; set; } = "";
    [Required(ErrorMessage = "Required"), MaxLength(100, ErrorMessage = "Maximum {1} characters"), Display(Name = "City")] public string City { get; set; } = "";

    public AssetSaveRequest ToRequest() => new(OwnerId, Type, Name, Address, PostalCode, City);
    public static AssetInput From(AssetDto a) => new() { OwnerId = a.OwnerId, Type = a.Type, Name = a.Name, Address = a.Address, PostalCode = a.PostalCode, City = a.City };
}

public class ExpenseInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Asset")] public int AssetId { get; set; }
    [DateInput, Display(Name = "Date")] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required(ErrorMessage = "Required"), MaxLength(500, ErrorMessage = "Maximum {1} characters"), Display(Name = "Description")] public string Description { get; set; } = "";
    [MaxLength(100, ErrorMessage = "Maximum {1} characters"), Display(Name = "Category")] public string? Category { get; set; }
    [Display(Name = "Amount excl. VAT")] public decimal AmountExclVat { get; set; }
    [Required(ErrorMessage = "Required"), Display(Name = "VAT rate")] public string VatRateName { get; set; } = "";
    [Display(Name = "Re-invoice to owner")] public bool Reinvoiceable { get; set; }

    public ExpenseSaveRequest ToRequest() => new(AssetId, Date, Description, Category, AmountExclVat, VatRateName, Reinvoiceable);
    public static ExpenseInput From(ExpenseDto e) => new() { AssetId = e.AssetId, Date = e.Date, Description = e.Description, Category = e.Category, AmountExclVat = e.AmountExclVat, VatRateName = e.VatRateName, Reinvoiceable = e.Reinvoiceable };
}

public class ActivityInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Asset")] public int AssetId { get; set; }
    [DateInput, Display(Name = "Date")] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Display(Name = "Kind")] public ActivityKind Kind { get; set; }
    [Required(ErrorMessage = "Required"), MaxLength(500, ErrorMessage = "Maximum {1} characters"), Display(Name = "Description")] public string Description { get; set; } = "";
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
    [Required(ErrorMessage = "Required"), MaxLength(50, ErrorMessage = "Maximum {1} characters"), Display(Name = "Name")] public string Name { get; set; } = "";
    [Range(0, 100, ErrorMessage = "Between 0 and 100"), Display(Name = "Percentage")] public decimal Percentage { get; set; }
    [DateInput, Display(Name = "Valid from")] public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [DateInput, Display(Name = "Valid to")] public DateOnly? ValidTo { get; set; }

    public VatRateSaveRequest ToRequest() => new(Name, Percentage, ValidFrom, ValidTo);
    public static VatRateInput From(VatRateDto v) => new() { Name = v.Name, Percentage = v.Percentage, ValidFrom = v.ValidFrom, ValidTo = v.ValidTo };
}
