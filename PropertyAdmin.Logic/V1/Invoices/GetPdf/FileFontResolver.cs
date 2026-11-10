using PdfSharp.Fonts;

namespace PropertyAdmin.Logic.V1.Invoices.GetPdf;

/// <summary>Resolves a sans-serif TrueType font from the operating system (Windows or Linux).</summary>
public sealed class FileFontResolver : IFontResolver
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
