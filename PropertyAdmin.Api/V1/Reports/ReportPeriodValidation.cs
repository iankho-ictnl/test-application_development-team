using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Api.Validation;

namespace PropertyAdmin.Api.V1.Reports;

/// <summary>The report period check of D-033, shared by both report requests: <c>from</c> and <c>to</c> are
/// required ISO dates (yyyy-MM-dd) and <c>to</c> is not before <c>from</c>; each error at its own field, in
/// the request language through <see cref="ValidationMessages"/> (D-049, D-052).</summary>
public static class ReportPeriodValidation
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string FromField = "from";
    public const string ToField = "to";

    public static IEnumerable<ValidationResult> Validate(string? from, string? to, ValidationContext context)
    {
        var localizer = context.GetService(typeof(IStringLocalizer<ValidationMessages>)) as IStringLocalizer<ValidationMessages>;
        var fromError = Check(from, out var fromDate);
        var toError = Check(to, out var toDate);
        if (fromError is not null)
            yield return new ValidationResult(Text(localizer, fromError), [FromField]);
        if (toError is not null)
            yield return new ValidationResult(Text(localizer, toError), [ToField]);
        if (fromError is null && toError is null && toDate < fromDate)
            yield return new ValidationResult(Text(localizer, ValidationMessages.PeriodReversed), [ToField]);
    }

    /// <summary>Parses a value that <see cref="Validate"/> accepted.</summary>
    public static DateOnly Parse(string? value) =>
        DateOnly.ParseExact(value ?? "", DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);

    private static string? Check(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
            return ValidationMessages.Required;
        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            ? null
            : ValidationMessages.DateFormat;
    }

    private static string Text(IStringLocalizer? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
