using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Contracts;

public enum AssetType { Building, Home }
public enum ActivityKind { Administration, Maintenance }
public enum PricingType { Hourly, Fixed }
public enum InvoiceStatus { Open, Paid }

// Auth
public record LoginRequest([Required] string UserName, [Required] string Password);
public record TwoFactorLoginRequest([Required] string ChallengeToken, [Required] string Code);
public record LoginResponse(bool RequiresTwoFactor, string? ChallengeToken, string? AccessToken, DateTime? ExpiresUtc);
public record TwoFactorSetupResponse(string SharedKey, string AuthenticatorUri, bool Enabled);
public record TwoFactorEnableRequest([Required] string Code);
public record TwoFactorEnableResponse(IReadOnlyList<string> RecoveryCodes);
public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

// Users and roles
public record UserDto(string Id, string UserName, string Email, bool TwoFactorEnabled, bool LockedOut, int? OwnerId, IReadOnlyList<string> Roles);
public record UserCreateRequest([Required, MaxLength(100)] string UserName, [Required, EmailAddress] string Email, [Required] string Password, int? OwnerId, IReadOnlyList<string> Roles);
public record UserUpdateRequest([Required, EmailAddress] string Email, int? OwnerId, IReadOnlyList<string> Roles, bool LockedOut);
public record RoleDto(string Name, IReadOnlyList<string> Permissions, int UserCount);
public record RoleSaveRequest([Required, MaxLength(50)] string Name, IReadOnlyList<string> Permissions);

// Owners and assets
public record OwnerDto(int Id, string Name, string? Email, string? Phone, string? Address, string? PostalCode, string? City);
public record OwnerSaveRequest([Required, MaxLength(200)] string Name, [EmailAddress, MaxLength(200)] string? Email, [MaxLength(50)] string? Phone, [MaxLength(200)] string? Address, [MaxLength(20)] string? PostalCode, [MaxLength(100)] string? City);
public record AssetDto(int Id, int OwnerId, string OwnerName, AssetType Type, string Name, string Address, string PostalCode, string City);
public record AssetSaveRequest([Range(1, int.MaxValue)] int OwnerId, AssetType Type, [Required, MaxLength(200)] string Name, [Required, MaxLength(200)] string Address, [Required, MaxLength(20)] string PostalCode, [Required, MaxLength(100)] string City);

// VAT
public record VatRateDto(int Id, string Name, decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);
public record VatRateSaveRequest([Required, MaxLength(50)] string Name, [Range(0, 100)] decimal Percentage, DateOnly ValidFrom, DateOnly? ValidTo);

// Expenses and activities
public record ExpenseDto(int Id, int AssetId, string AssetName, DateOnly Date, string Description, string? Category, decimal AmountExclVat, string VatRateName, decimal VatPercentage, decimal VatAmount, decimal AmountInclVat, bool Reinvoiceable, int? InvoiceId);
public record ExpenseSaveRequest([Range(1, int.MaxValue)] int AssetId, DateOnly Date, [Required, MaxLength(500)] string Description, [MaxLength(100)] string? Category, [Range(0, 999999999)] decimal AmountExclVat, [Required] string VatRateName, bool Reinvoiceable);
public record ActivityDto(int Id, int AssetId, string AssetName, DateOnly Date, ActivityKind Kind, string Description, PricingType PricingType, decimal? Hours, decimal? HourlyRate, decimal? FixedPrice, decimal AmountExclVat, string VatRateName, decimal VatPercentage, decimal VatAmount, decimal AmountInclVat, int? InvoiceId);
public record ActivitySaveRequest([Range(1, int.MaxValue)] int AssetId, DateOnly Date, ActivityKind Kind, [Required, MaxLength(500)] string Description, PricingType PricingType, [Range(0, 100000)] decimal? Hours, [Range(0, 1000000)] decimal? HourlyRate, [Range(0, 999999999)] decimal? FixedPrice, [Required] string VatRateName);

// Invoices
public record InvoiceLineDto(string Description, decimal AmountExclVat, decimal VatPercentage, decimal VatAmount, decimal AmountInclVat);
public record VatSummaryDto(decimal VatPercentage, decimal Base, decimal VatAmount);
public record InvoiceDto(int Id, string Number, int OwnerId, string OwnerName, DateOnly Date, DateOnly DueDate, InvoiceStatus Status, DateOnly? PaidDate, decimal TotalExclVat, decimal TotalVat, decimal TotalInclVat, IReadOnlyList<InvoiceLineDto> Lines, IReadOnlyList<VatSummaryDto> VatSummary);
public record InvoiceCreateRequest([Range(1, int.MaxValue)] int OwnerId, DateOnly Date, [Range(0, 365)] int PaymentTermDays);
public record UninvoicedDto(IReadOnlyList<ActivityDto> Activities, IReadOnlyList<ExpenseDto> Expenses);
public record MarkPaidRequest(DateOnly PaidDate);

// Reports
public record AssetResultDto(int AssetId, string AssetName, string OwnerName, decimal Income, decimal Expenses, decimal Result);
public record OwnerOverviewDto(int OwnerId, string OwnerName, int AssetCount, decimal Income, decimal Expenses, int OpenInvoiceCount, decimal OpenInvoiceAmount);

public record ProblemMessage(string Message);
