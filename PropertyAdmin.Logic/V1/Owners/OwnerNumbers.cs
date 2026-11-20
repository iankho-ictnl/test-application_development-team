using System.Text.RegularExpressions;

namespace PropertyAdmin.Logic.V1.Owners;

/// <summary>Normalization and format of the owner's KvK and BTW numbers (D-041, Q-088). No VIES check (Q-051).</summary>
public static partial class OwnerNumbers
{
    /// <summary>The 27 EU member-state VAT prefixes; Greece is EL (Your Europe, VIES, checked 2026-11-20).</summary>
    public static readonly IReadOnlySet<string> VatPrefixes = new HashSet<string>(StringComparer.Ordinal)
    {
        "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "EL", "ES", "FI", "FR", "HR", "HU", "IE", "IT",
        "LT", "LU", "LV", "MT", "NL", "PL", "PT", "RO", "SE", "SI", "SK",
    };

    /// <summary>Trimmed KvK number, or null when empty.</summary>
    public static string? NormalizeKvk(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>BTW number without spaces and dots, upper case, or null when empty.</summary>
    public static string? NormalizeVat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Replace(" ", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    /// <summary>Empty, or exactly 8 digits.</summary>
    public static bool IsValidKvk(string? value)
    {
        var normalized = NormalizeKvk(value);
        return normalized is null || KvkPattern().IsMatch(normalized);
    }

    /// <summary>Empty, or an EU prefix with 2 to 12 letters or digits; NL needs 9 digits, B and 2 digits.</summary>
    public static bool IsValidVat(string? value)
    {
        var normalized = NormalizeVat(value);
        if (normalized is null) return true;
        if (!VatPattern().IsMatch(normalized)) return false;
        var prefix = normalized[..2];
        if (!VatPrefixes.Contains(prefix)) return false;
        return prefix != "NL" || DutchVatPattern().IsMatch(normalized);
    }

    [GeneratedRegex("^[0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex KvkPattern();

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{2,12}$", RegexOptions.CultureInvariant)]
    private static partial Regex VatPattern();

    [GeneratedRegex("^NL[0-9]{9}B[0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex DutchVatPattern();
}
