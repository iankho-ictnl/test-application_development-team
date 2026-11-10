namespace PropertyAdmin.Logic.V1.Invoices.MarkPaid;

public sealed record Command(int Id, DateOnly PaidDate);
