using Microsoft.AspNetCore.Identity;

namespace PropertyAdmin.Data;

public class ApplicationUser : IdentityUser
{
    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }
}

public class Owner
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Street { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    /// <summary>Chamber of Commerce (KvK) number, exactly 8 digits when given (D-041).</summary>
    public string? KvkNumber { get; set; }
    /// <summary>EU VAT (BTW) number, normalized, when given (D-041, Q-088).</summary>
    public string? VatNumber { get; set; }
    /// <summary>Owners are never deleted (Q-026); the 7-year anonymization term counts from this date (REQ-024).</summary>
    public DateOnly? InactiveSince { get; set; }
    public List<Asset> Assets { get; set; } = [];
}

public class Asset
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public AssetType Type { get; set; }
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";
    public string HouseNumber { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class VatRate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Percentage { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class Expense
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string Description { get; set; } = "";
    public string? Category { get; set; }
    public decimal AmountExclVat { get; set; }
    public string VatRateName { get; set; } = "";
    public decimal VatPercentage { get; set; }
    public bool Reinvoiceable { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}

public class Activity
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;
    public DateOnly Date { get; set; }
    public ActivityKind Kind { get; set; }
    public string Description { get; set; } = "";
    public PricingType PricingType { get; set; }
    public decimal? Hours { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? FixedPrice { get; set; }
    public decimal AmountExclVat { get; set; }
    public string VatRateName { get; set; } = "";
    public decimal VatPercentage { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public int OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public DateOnly Date { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateOnly? PaidDate { get; set; }
    public List<InvoiceLine> Lines { get; set; } = [];
}

public enum InvoiceLineSource { Activity, Expense }

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public decimal AmountExclVat { get; set; }
    public decimal VatPercentage { get; set; }
    public InvoiceLineSource SourceType { get; set; }
    public int SourceId { get; set; }
}
