namespace PropertyAdmin.Logic.V1.Invoices.GetPdf;

public sealed record Result(string? FileName, byte[]? Content)
{
    public bool Found => Content is not null;
}
