extern alias Frontend;

using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ModelBindingMessages = Frontend::test_application_for_development_team.Services.ModelBindingMessages;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// BUG-14, REQ-011 (REQ-007 invoice form and the other forms with non-nullable date and number fields): an empty or
/// unreadable value gets a Dutch or English model binding message from SharedResource, never the framework's English
/// default ("The value '' is invalid.").
/// </summary>
public sealed class ModelBindingMessageTests
{
    public static TheoryData<string, Func<DefaultModelBindingMessageProvider, string>> Accessors() => new()
    {
        { "Required", p => p.ValueMustNotBeNullAccessor("") },
        { "Required", p => p.MissingBindRequiredValueAccessor("Date") },
        { "Required", p => p.MissingKeyOrValueAccessor() },
        { "Required", p => p.MissingRequestBodyRequiredValueAccessor() },
        { "Invalid value", p => p.AttemptedValueIsInvalidAccessor("x", "Date") },
        { "Invalid value", p => p.NonPropertyAttemptedValueIsInvalidAccessor("x") },
        { "Invalid value", p => p.UnknownValueIsInvalidAccessor("Date") },
        { "Invalid value", p => p.NonPropertyUnknownValueIsInvalidAccessor() },
        { "Invalid value", p => p.ValueIsInvalidAccessor("x") },
        { "Must be a number", p => p.ValueMustBeANumberAccessor("Amount") },
        { "Must be a number", p => p.NonPropertyValueMustBeANumberAccessor() },
    };

    [Theory]
    [MemberData(nameof(Accessors))]
    public void Every_model_binding_message_is_in_Dutch_and_in_English(string key, Func<DefaultModelBindingMessageProvider, string> accessor)
    {
        var provider = ConfiguredProvider();
        var dutch = WithCulture("nl-NL", () => accessor(provider));
        var english = WithCulture("en-US", () => accessor(provider));

        Assert.Equal(key, english);
        Assert.NotEqual(english, dutch);
        Assert.DoesNotContain("'", dutch, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nl-NL", "Verplicht", "Ongeldige waarde", "Moet een getal zijn")]
    [InlineData("en-US", "Required", "Invalid value", "Must be a number")]
    public void An_empty_or_unreadable_date_or_number_shows_the_translated_message(string culture, string required, string invalid, string number)
    {
        var provider = ConfiguredProvider();

        Assert.Equal(required, WithCulture(culture, () => provider.ValueMustNotBeNullAccessor("")));
        Assert.Equal(invalid, WithCulture(culture, () => provider.AttemptedValueIsInvalidAccessor("31-02-2026", "Date")));
        Assert.Equal(number, WithCulture(culture, () => provider.ValueMustBeANumberAccessor("Amount")));
    }

    [Fact]
    public void The_frontend_registers_the_localized_messages_for_MVC()
    {
        using var factory = new WebApplicationFactory<Frontend::Program>()
            .WithWebHostBuilder(b => b.UseSetting("Api:BaseUrl", "http://localhost:1/"));
        var provider = factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value.ModelBindingMessageProvider;

        Assert.Equal("Verplicht", WithCulture("nl-NL", () => provider.ValueMustNotBeNullAccessor("")));
        Assert.Equal("Required", WithCulture("en-US", () => provider.ValueMustNotBeNullAccessor("")));
    }

    private static DefaultModelBindingMessageProvider ConfiguredProvider()
    {
        var options = new MvcOptions();
        new ModelBindingMessages(Factory()).Configure(options);
        return options.ModelBindingMessageProvider;
    }

    private static IStringLocalizerFactory Factory() =>
        new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizerFactory>();

    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try { return action(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
}
