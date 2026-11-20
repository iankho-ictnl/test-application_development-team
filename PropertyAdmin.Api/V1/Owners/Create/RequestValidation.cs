using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Owners.Create;

/// <summary>KvK and BTW format checks (D-041, Q-088) in the request language (T-114); the required fields are checked by their attributes.</summary>
public static class RequestValidation
{
    public static IEnumerable<ValidationResult> Validate(Request request, ValidationContext context) =>
        OwnerNumberValidation.ValidateNumbers(request.KvkNumber, request.VatNumber, context);
}
