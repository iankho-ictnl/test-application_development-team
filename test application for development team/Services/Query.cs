using System.Globalization;

namespace test_application_for_development_team.Services;

public static class Query
{
    public static string Build(string path, params (string Key, object? Value)[] values)
    {
        var parts = values.Where(v => v.Value is not null).Select(v => $"{v.Key}={Uri.EscapeDataString(Format(v.Value!))}").ToList();
        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }

    private static string Format(object value) => value switch
    {
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };
}
