using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PropertyAdmin.Api.V1.Reports.GetAssetResults;

/// <summary>The report period (D-033): <c>from</c> and <c>to</c> as ISO dates (yyyy-MM-dd), both required.
/// Strings, so a missing or unparseable value reaches <see cref="RequestValidation"/> and gets a localized
/// error at its field instead of the framework's binding message.</summary>
public sealed record Request(
    [FromQuery(Name = ReportPeriodValidation.FromField)] string? From,
    [FromQuery(Name = ReportPeriodValidation.ToField)] string? To) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        RequestValidation.Validate(this, validationContext);
}
