using System.Globalization;
using Microsoft.Extensions.Options;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PropertyAdmin.Contracts;

namespace PropertyAdmin.Api.Services;

public class CompanyOptions
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

public class InvoicePdfService
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");
    private readonly CompanyOptions _company;

    public InvoicePdfService(IOptions<CompanyOptions> company)
    {
        _company = company.Value;
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new FileFontResolver();
    }

    public byte[] Render(InvoiceDto invoice, OwnerDto owner)
    {
        var doc = new Document();
        doc.Info.Title = $"Factuur {invoice.Number}";
        var normal = doc.Styles[StyleNames.Normal]!;
        normal.Font.Name = FileFontResolver.Family;
        normal.Font.Size = 10;

        var section = doc.AddSection();
        section.PageSetup = doc.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2);

        var header = section.AddParagraph(_company.Name);
        header.Format.Font.Size = 16;
        header.Format.Font.Bold = true;
        section.AddParagraph($"{_company.Address}\n{_company.PostalCode} {_company.City}\n{_company.Email}");
        section.AddParagraph($"KvK: {_company.ChamberOfCommerce}   Btw-nr: {_company.VatNumber}   IBAN: {_company.Iban}");

        var to = section.AddParagraph();
        to.Format.SpaceBefore = Unit.FromCentimeter(1);
        to.AddText($"{owner.Name}\n{owner.Address}\n{owner.PostalCode} {owner.City}");

        var title = section.AddParagraph($"Factuur {invoice.Number}");
        title.Format.SpaceBefore = Unit.FromCentimeter(1);
        title.Format.Font.Size = 14;
        title.Format.Font.Bold = true;
        section.AddParagraph($"Factuurdatum: {invoice.Date.ToString("dd-MM-yyyy", Nl)}   Vervaldatum: {invoice.DueDate.ToString("dd-MM-yyyy", Nl)}");

        var table = section.AddTable();
        table.Format.SpaceBefore = Unit.FromCentimeter(0.5);
        table.Borders.Bottom.Width = 0.5;
        table.AddColumn(Unit.FromCentimeter(9.5));
        foreach (var w in new[] { 2.5, 1.5, 3.5 })
            table.AddColumn(Unit.FromCentimeter(w)).Format.Alignment = ParagraphAlignment.Right;

        var head = table.AddRow();
        head.HeadingFormat = true;
        head.Format.Font.Bold = true;
        head.Cells[0].AddParagraph("Omschrijving");
        head.Cells[1].AddParagraph("Bedrag");
        head.Cells[2].AddParagraph("Btw %");
        head.Cells[3].AddParagraph("Incl. btw");

        foreach (var l in invoice.Lines)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(l.Description);
            row.Cells[1].AddParagraph(Eur(l.AmountExclVat));
            row.Cells[2].AddParagraph(l.VatPercentage.ToString("0.##", Nl));
            row.Cells[3].AddParagraph(Eur(l.AmountInclVat));
        }

        var totals = section.AddTable();
        totals.Format.SpaceBefore = Unit.FromCentimeter(0.5);
        totals.AddColumn(Unit.FromCentimeter(13.5)).Format.Alignment = ParagraphAlignment.Right;
        totals.AddColumn(Unit.FromCentimeter(3.5)).Format.Alignment = ParagraphAlignment.Right;
        AddTotal(totals, "Totaal excl. btw", invoice.TotalExclVat);
        foreach (var v in invoice.VatSummary)
            AddTotal(totals, $"Btw {v.VatPercentage.ToString("0.##", Nl)}% over {Eur(v.Base)}", v.VatAmount);
        AddTotal(totals, "Totaal te betalen", invoice.TotalInclVat, bold: true);

        var pay = section.AddParagraph($"Gelieve het totaalbedrag voor {invoice.DueDate.ToString("dd-MM-yyyy", Nl)} over te maken op {_company.Iban} o.v.v. factuurnummer {invoice.Number}.");
        pay.Format.SpaceBefore = Unit.FromCentimeter(1);

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();
        using var ms = new MemoryStream();
        renderer.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }

    private static void AddTotal(Table t, string label, decimal amount, bool bold = false)
    {
        var row = t.AddRow();
        row.Format.Font.Bold = bold;
        row.Cells[0].AddParagraph(label);
        row.Cells[1].AddParagraph(Eur(amount));
    }

    private static string Eur(decimal v) => v.ToString("C2", Nl);
}

/// <summary>Resolves a sans-serif TrueType font from the operating system (Windows or Linux).</summary>
public class FileFontResolver : IFontResolver
{
    public const string Family = "InvoiceSans";

    private static readonly string[][] Candidates =
    [
        [@"C:\Windows\Fonts\arial.ttf", "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"],
        [@"C:\Windows\Fonts\arialbd.ttf", "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"],
    ];

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? "sans-bold" : "sans-regular");

    public byte[]? GetFont(string faceName)
    {
        var list = faceName == "sans-bold" ? Candidates[1] : Candidates[0];
        var path = list.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("No TrueType font found for PDF rendering. Install fonts-liberation in the container.");
        return File.ReadAllBytes(path);
    }
}
