namespace PropertyAdmin.Logic.V1.Invoices.Models;

public sealed record InvoiceCreateModel(int OwnerId, DateOnly Date, int PaymentTermDays);
