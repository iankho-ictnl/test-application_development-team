using System.ComponentModel.DataAnnotations;
using PropertyAdmin.Logic.V1.Owners;

namespace PropertyAdmin.Api.V1.Owners.Create;

/// <summary>KvK and BTW format checks (D-041, Q-088); the required fields are checked by their attributes.</summary>
public static class RequestValidation
{
    public static IEnumerable<ValidationResult> Validate(Request request)
    {
        if (!OwnerNumbers.IsValidKvk(request.KvkNumber))
            yield return new ValidationResult("KvK-nummer moet uit 8 cijfers bestaan.", [nameof(Request.KvkNumber)]);
        if (!OwnerNumbers.IsValidVat(request.VatNumber))
            yield return new ValidationResult("Btw-nummer heeft geen geldig EU-formaat.", [nameof(Request.VatNumber)]);
    }
}
