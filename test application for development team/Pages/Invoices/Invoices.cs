using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Contracts;
using test_application_for_development_team.Services;

namespace test_application_for_development_team.Pages.Invoices;

[Authorize(Policy = FrontendPolicies.Read)]
public class IndexModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<InvoiceDto> Items { get; set; } = [];
    public List<SelectListItem> Owners { get; set; } = [];
    public List<SelectListItem> Statuses { get; set; } = [];
    [BindProperty(SupportsGet = true)] public int? OwnerId { get; set; }
    [BindProperty(SupportsGet = true)] public InvoiceStatus? Status { get; set; }

    public async Task OnGetAsync()
    {
        Owners = await OwnerOptionsAsync();
        Statuses = EnumOptions<InvoiceStatus>();
        Items = await Api.GetAsync<List<InvoiceDto>>(Query.Build("api/invoices", ("ownerId", OwnerId), ("status", Status)));
    }
}

[Authorize(Policy = FrontendPolicies.Read)]
public class DetailsModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public InvoiceDto Invoice { get; set; } = null!;
    [BindProperty] public DateOnly PaidDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public async Task OnGetAsync(int id) => Invoice = await Api.GetAsync<InvoiceDto>($"api/invoices/{id}");

    public async Task<IActionResult> OnGetPdfAsync(int id)
    {
        var (bytes, name) = await Api.GetFileAsync($"api/invoices/{id}/pdf");
        return File(bytes, "application/pdf", name);
    }

    public async Task<IActionResult> OnPostPayAsync(int id)
    {
        if (!User.Has(Permissions.Invoices)) return Forbid();
        var r = await Api.PostAsync($"api/invoices/{id}/pay", new MarkPaidRequest(PaidDate));
        if (!r.Ok)
        {
            AddApiError(r);
            await OnGetAsync(id);
            return Page();
        }
        Done("Invoice marked as paid.");
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.Has(Permissions.Invoices)) return Forbid();
        var r = await Api.DeleteAsync($"api/invoices/{id}");
        if (!r.Ok)
        {
            AddApiError(r);
            await OnGetAsync(id);
            return Page();
        }
        Done("Invoice deleted.");
        return RedirectToPage("Index");
    }
}

[Authorize(Policy = Permissions.Invoices)]
public class CreateModel(ApiClient api, IStringLocalizer<SharedResource> l) : ApiPageModel(api, l)
{
    public List<SelectListItem> Owners { get; set; } = [];
    public UninvoicedDto? Uninvoiced { get; set; }

    [BindProperty(SupportsGet = true), Range(1, int.MaxValue, ErrorMessage = "Required"), Display(Name = "Owner")]
    public int OwnerId { get; set; }

    [BindProperty, Display(Name = "Invoice date")] public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty, Range(0, 365), Display(Name = "Payment term (days)")] public int PaymentTermDays { get; set; } = 14;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var r = await Api.PostAsync<InvoiceDto>("api/invoices", new InvoiceCreateRequest(OwnerId, Date, PaymentTermDays));
            if (r.Ok && r.Value is not null)
            {
                Done("Invoice created.");
                return RedirectToPage("Details", new { id = r.Value.Id });
            }
            AddApiError(r);
        }
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Owners = await OwnerOptionsAsync();
        if (OwnerId > 0) Uninvoiced = await Api.GetAsync<UninvoicedDto>($"api/invoices/uninvoiced/{OwnerId}");
    }
}
