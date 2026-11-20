using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Logic.V1.Owners;

namespace PropertyAdmin.Api.V1.Owners;

/// <summary>
/// The owner validation messages in the request language (REQ-011, T-114, T-127): the keys are the English
/// texts, the Dutch texts are in Resources/V1/Owners/OwnerMessages.nl.resx. The API's UI culture comes
/// from the query parameter <c>lang</c> (nl-NL or en-US), Dutch when it is missing or not supported
/// (D-049, Program.cs). The data annotation messages of the owner requests are localized through this
/// class as well (AddDataAnnotationsLocalization in Program.cs).
/// </summary>
public sealed class OwnerMessages
{
    public const string Required = "This field is required.";
    public const string EmailFormat = "This is not a valid email address.";
    /// <summary>{1} is the maximum length (MaxLengthAttribute's second format argument).</summary>
    public const string MaxLength = "This field can contain at most {1} characters.";
    /// <summary>For a Range or EnumDataType check (asset owner and type, T-053).</summary>
    public const string InvalidChoice = "Choose a valid value.";

    public const string KvkFormat = "The KvK number must consist of 8 digits.";
    public const string VatFormat = "The VAT number is not in a valid EU format.";

    /// <summary>KvK and BTW format checks (D-041, Q-088), shared by the Create and Update requests.</summary>
    public static IEnumerable<ValidationResult> ValidateNumbers(string? kvkNumber, string? vatNumber, ValidationContext context)
    {
        var localizer = context.GetService(typeof(IStringLocalizer<OwnerMessages>)) as IStringLocalizer<OwnerMessages>;
        if (!OwnerNumbers.IsValidKvk(kvkNumber))
            yield return new ValidationResult(Text(localizer, KvkFormat), ["KvkNumber"]);
        if (!OwnerNumbers.IsValidVat(vatNumber))
            yield return new ValidationResult(Text(localizer, VatFormat), ["VatNumber"]);
    }

    private static string Text(IStringLocalizer? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
