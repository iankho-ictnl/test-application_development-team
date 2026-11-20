using System.Globalization;

namespace test_application_for_development_team.Services;

/// <summary>
/// Builds the request cultures of the frontend (D-010). Money is always in euro (REQ-018), whatever the
/// language: the culture keeps its own number and date formats (en-US: €1,234.50; nl-NL: € 1.234,50),
/// only its currency symbol is the euro sign. The culture is shared by every request, so it is read-only.
/// </summary>
public static class AppCultures
{
    public const string EuroSymbol = "€";

    public static CultureInfo Create(string name) => WithEuro(new CultureInfo(name));

    /// <summary>A read-only copy of <paramref name="culture"/> with the euro as its currency symbol.</summary>
    public static CultureInfo WithEuro(CultureInfo culture)
    {
        var copy = (CultureInfo)culture.Clone(); // a clone is writable, also of a read-only culture
        copy.NumberFormat.CurrencySymbol = EuroSymbol;
        return CultureInfo.ReadOnly(copy);
    }
}
