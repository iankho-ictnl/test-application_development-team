namespace PropertyAdmin.Logic.V1.Invoices.GetPdf;

public sealed class CompanyOptions
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string VatNumber { get; set; } = "";
    public string ChamberOfCommerce { get; set; } = "";
    public string Iban { get; set; } = "";
    public string Email { get; set; } = "";
}
