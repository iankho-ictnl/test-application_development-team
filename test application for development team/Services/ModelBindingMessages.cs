using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace test_application_for_development_team.Services;

/// <summary>
/// Localized model binding messages (BUG-14, D-010): an empty or unreadable value of a non-nullable date or
/// number field gets a Dutch or English message from SharedResource, like the data annotation messages, instead
/// of the framework's English default ("The value '' is invalid."). The localizer is read when the message is
/// made, so the message follows the culture of the request. The attempted value is never echoed.
/// </summary>
public sealed class ModelBindingMessages(IStringLocalizerFactory localizerFactory) : IConfigureOptions<MvcOptions>
{
    public const string Required = "Required";
    public const string InvalidValue = "Invalid value";
    public const string MustBeANumber = "Must be a number";

    public void Configure(MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var l = localizerFactory.Create(typeof(SharedResource));
        var p = options.ModelBindingMessageProvider;
        p.SetValueMustNotBeNullAccessor(_ => l[Required]);
        p.SetMissingBindRequiredValueAccessor(_ => l[Required]);
        p.SetMissingKeyOrValueAccessor(() => l[Required]);
        p.SetMissingRequestBodyRequiredValueAccessor(() => l[Required]);
        p.SetAttemptedValueIsInvalidAccessor((_, _) => l[InvalidValue]);
        p.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => l[InvalidValue]);
        p.SetUnknownValueIsInvalidAccessor(_ => l[InvalidValue]);
        p.SetNonPropertyUnknownValueIsInvalidAccessor(() => l[InvalidValue]);
        p.SetValueIsInvalidAccessor(_ => l[InvalidValue]);
        p.SetValueMustBeANumberAccessor(_ => l[MustBeANumber]);
        p.SetNonPropertyValueMustBeANumberAccessor(() => l[MustBeANumber]);
    }
}
