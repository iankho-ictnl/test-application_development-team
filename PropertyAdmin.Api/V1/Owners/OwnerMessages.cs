using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Logic.V1.Owners;

namespace PropertyAdmin.Api.V1.Owners;

/// <summary>
/// The owner validation messages in the request language (REQ-011, T-114): the keys are the English
/// texts, the Dutch texts are in Resources/V1/Owners/OwnerMessages.nl.resx. The API's UI culture comes
/// from the Accept-Language header, Dutch when it is missing (Program.cs).
/// </summary>
public sealed class OwnerMessages
{
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
