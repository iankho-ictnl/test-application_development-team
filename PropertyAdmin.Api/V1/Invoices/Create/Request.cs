using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Invoices.Create;

public sealed record Request([Range(1, int.MaxValue)] int OwnerId, DateOnly Date, [Range(0, 365)] int PaymentTermDays);
