extern alias Frontend;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Frontend::test_application_for_development_team;
using OwnerInput = Frontend::test_application_for_development_team.Pages.OwnerInput;

namespace PropertyAdmin.Tests.Frontend;

/// <summary>
/// BUG-12, REQ-011 (REQ-002 to REQ-006, REQ-012 forms): every validation attribute on the frontend page models has its
/// own message, translated in Dutch and English, so the framework's English default text never reaches the screen.
/// </summary>
public sealed class ValidationMessageTests
{
    /// <summary>Every validation attribute on a property of a frontend type, except data types (they never fail).</summary>
    public static TheoryData<string, string> Attributes()
    {
        var data = new TheoryData<string, string>();
        var types = typeof(SharedResource).Assembly.GetTypes();
        foreach (var type in types)
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        foreach (var attribute in property.GetCustomAttributes<ValidationAttribute>(inherit: true))
        {
            if (attribute is DataTypeAttribute) continue;
            data.Add($"{type.Name}.{property.Name} {attribute.GetType().Name}", attribute.ErrorMessage ?? "");
        }
        return data;
    }

    [Fact]
    public void The_frontend_has_validation_attributes_to_check() => Assert.True(Attributes().Count > 20);

    [Theory]
    [MemberData(nameof(Attributes))]
    public void Every_validation_attribute_has_a_message_in_Dutch_and_English(string where, string message)
    {
        Assert.False(string.IsNullOrEmpty(message), $"{where} has no ErrorMessage, so the framework's English text is shown.");
        foreach (var culture in new[] { "nl-NL", "en-US" })
        {
            var text = WithCulture(culture, () => RealLocalizer()[message]);
            Assert.False(text.ResourceNotFound, $"No {culture} resource entry for \"{message}\" ({where}).");
        }
    }

    [Theory]
    [InlineData("nl-NL", "Maximaal 200 tekens")]
    [InlineData("en-US", "Maximum 200 characters")]
    public void The_maximum_length_message_of_the_owner_name_is_in_the_users_language(string culture, string expected)
    {
        var attributes = new Dictionary<string, string>();
        var provider = new EmptyModelMetadataProvider();
        var metadata = provider.GetMetadataForProperty(typeof(OwnerInput), nameof(OwnerInput.Name));
        var maxLength = typeof(OwnerInput).GetProperty(nameof(OwnerInput.Name))!.GetCustomAttribute<MaxLengthAttribute>()!;
        var adapter = new ValidationAttributeAdapterProvider().GetAttributeAdapter(maxLength, RealLocalizer())!;
        var context = new ClientModelValidationContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            metadata, provider, attributes);

        WithCulture(culture, () => { adapter.AddValidation(context); return 0; });

        Assert.Equal(expected, attributes["data-val-maxlength"]);
        Assert.Equal("200", attributes["data-val-maxlength-max"]);
    }

    private static T WithCulture<T>(string culture, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try { return action(); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static IStringLocalizer<SharedResource> RealLocalizer() =>
        new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResource>>();
}
