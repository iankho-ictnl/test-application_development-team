using System.ComponentModel.DataAnnotations;

namespace test_application_for_development_team.Pages;

/// <summary>
/// Marks a <see cref="DateOnly"/> property as a date field (BUG-8, REQ-021). The input tag helper has no
/// input type for <see cref="DateOnly"/>, so without this the field renders as text in the page culture,
/// which the model binder refuses on post. With it the field renders as <c>type="date"</c> with the HTML
/// date value (yyyy-MM-dd); the browser shows it in the user's language (dd-mm-jjjj in Dutch) and posts
/// yyyy-MM-dd back, which binds in every culture.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DateInputAttribute : DataTypeAttribute
{
    public const string HtmlDateFormat = "{0:yyyy-MM-dd}";

    public DateInputAttribute() : base(DataType.Date)
    {
        DisplayFormat = new DisplayFormatAttribute { DataFormatString = HtmlDateFormat, ApplyFormatInEditMode = true };
    }
}
