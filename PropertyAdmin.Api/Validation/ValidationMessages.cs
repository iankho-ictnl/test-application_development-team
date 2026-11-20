namespace PropertyAdmin.Api.Validation;

/// <summary>
/// The validation messages of all API versions and features in the request language (REQ-011, T-114,
/// T-127, T-140): the keys are the English texts, the Dutch texts are in
/// Resources/Validation/ValidationMessages.nl.resx. The API's UI culture comes from the query parameter
/// <c>lang</c> (nl-NL or en-US), Dutch when it is missing or not supported (D-049, Program.cs). The data
/// annotation messages of the requests are localized through this class (AddDataAnnotationsLocalization
/// in Program.cs, D-052).
/// </summary>
public sealed class ValidationMessages
{
    public const string Required = "This field is required.";
    public const string EmailFormat = "This is not a valid email address.";
    /// <summary>{1} is the maximum length (MaxLengthAttribute's second format argument).</summary>
    public const string MaxLength = "This field can contain at most {1} characters.";
    /// <summary>For a Range or EnumDataType check (asset owner and type, T-053).</summary>
    public const string InvalidChoice = "Choose a valid value.";

    public const string KvkFormat = "The KvK number must consist of 8 digits.";
    public const string VatFormat = "The VAT number is not in a valid EU format.";
}
