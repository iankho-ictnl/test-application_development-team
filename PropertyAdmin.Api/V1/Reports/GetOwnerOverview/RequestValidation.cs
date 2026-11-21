using System.ComponentModel.DataAnnotations;

namespace PropertyAdmin.Api.V1.Reports.GetOwnerOverview;

/// <summary>The report period check of D-033 (missing, invalid, reversed: 400 at the field, localized by lang).</summary>
public static class RequestValidation
{
    public static IEnumerable<ValidationResult> Validate(Request request, ValidationContext context) =>
        ReportPeriodValidation.Validate(request.From, request.To, context);
}
