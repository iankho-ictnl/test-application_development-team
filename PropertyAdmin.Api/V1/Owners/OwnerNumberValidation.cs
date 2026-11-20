using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using PropertyAdmin.Api.Validation;
using PropertyAdmin.Logic.V1.Owners;

namespace PropertyAdmin.Api.V1.Owners;

/// <summary>KvK and BTW format checks (D-041, Q-088), shared by the owner Create and Update requests, with
/// the messages of <see cref="ValidationMessages"/> in the request language (T-114, T-140).</summary>
public static class OwnerNumberValidation
{
    public static IEnumerable<ValidationResult> ValidateNumbers(string? kvkNumber, string? vatNumber, ValidationContext context)
    {
        var localizer = context.GetService(typeof(IStringLocalizer<ValidationMessages>)) as IStringLocalizer<ValidationMessages>;
        if (!OwnerNumbers.IsValidKvk(kvkNumber))
            yield return new ValidationResult(Text(localizer, ValidationMessages.KvkFormat), ["KvkNumber"]);
        if (!OwnerNumbers.IsValidVat(vatNumber))
            yield return new ValidationResult(Text(localizer, ValidationMessages.VatFormat), ["VatNumber"]);
    }

    private static string Text(IStringLocalizer? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
