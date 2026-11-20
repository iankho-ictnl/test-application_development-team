using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Invoices.GetAll;

public sealed record Request(int? OwnerId, [EnumDataType(typeof(InvoiceStatus))] InvoiceStatus? Status);
