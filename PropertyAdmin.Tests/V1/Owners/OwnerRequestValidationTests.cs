using System.ComponentModel.DataAnnotations;
using OwnersApi = PropertyAdmin.Api.V1.Owners;

namespace PropertyAdmin.Tests.V1.Owners;

/// <summary>REQ-002, D-041, Q-088: one test per validation rule of the owner Create and Update requests.</summary>
public sealed class OwnerRequestValidationTests
{
    private static OwnersApi.Create.Request Valid(string? kvk = null, string? vat = null) =>
        new("Name", "owner@example.test", "0612345678", "Road", "1A", "1000 AA", "City", "Nederland", kvk, vat);

    [Fact]
    public void A_complete_request_without_kvk_and_btw_is_valid() => Assert.Empty(Errors(Valid()));

    [Fact]
    public void A_foreign_address_is_valid() =>
        Assert.Empty(Errors(Valid() with { Street = "Rue de la Loi", HouseNumber = "16", PostalCode = "B-1000", City = "Bruxelles", Country = "Belgique" }));

    [Theory]
    [InlineData("Name")]
    [InlineData("Email")]
    [InlineData("Phone")]
    [InlineData("Street")]
    [InlineData("HouseNumber")]
    [InlineData("PostalCode")]
    [InlineData("City")]
    [InlineData("Country")]
    public void Each_required_field_empty_gives_an_error_at_that_field(string field)
    {
        var request = field switch
        {
            "Name" => Valid() with { Name = " " },
            "Email" => Valid() with { Email = "" },
            "Phone" => Valid() with { Phone = " " },
            "Street" => Valid() with { Street = "" },
            "HouseNumber" => Valid() with { HouseNumber = " " },
            "PostalCode" => Valid() with { PostalCode = "" },
            "City" => Valid() with { City = " " },
            _ => Valid() with { Country = "" },
        };

        Assert.Equal([field], Errors(request));
    }

    [Fact]
    public void An_invalid_email_gives_an_error_at_email() =>
        Assert.Equal(["Email"], Errors(Valid() with { Email = "no-email" }));

    [Theory]
    [InlineData("12345678")]
    [InlineData(" 12345678 ")]
    [InlineData("")]
    public void Kvk_empty_or_eight_digits_is_valid(string kvk) => Assert.Empty(Errors(Valid(kvk: kvk)));

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("1234567A")]
    public void Kvk_other_than_eight_digits_gives_an_error_at_kvk(string kvk) =>
        Assert.Equal(["KvkNumber"], Errors(Valid(kvk: kvk)));

    [Theory]
    [InlineData("NL123456789B01")]
    [InlineData("nl 1234.56789.b01")]
    [InlineData("DE123456789")]
    [InlineData("EL123456789")]
    [InlineData("ATU12345678")]
    [InlineData("FR12345678901")]
    [InlineData("")]
    public void Btw_empty_or_an_eu_format_is_valid(string vat) => Assert.Empty(Errors(Valid(vat: vat)));

    [Theory]
    [InlineData("XX123456")]
    [InlineData("GR123456789")]
    [InlineData("NL123")]
    [InlineData("NL123456789X01")]
    [InlineData("DE1")]
    [InlineData("DE1234567890123")]
    [InlineData("DE-12345678")]
    public void Btw_with_an_unknown_prefix_or_wrong_format_gives_an_error_at_btw(string vat) =>
        Assert.Equal(["VatNumber"], Errors(Valid(vat: vat)));

    [Fact]
    public void Update_request_checks_kvk_and_btw_as_well()
    {
        var request = new OwnersApi.Update.Request("Name", "owner@example.test", "06", "Road", "1", "1000 AA", "City", "Nederland", "123", "NL123");

        Assert.Equal(["KvkNumber", "VatNumber"], Errors(request));
    }

    /// <summary>Checks the attributes on the record's constructor parameters (where MVC reads them), then IValidatableObject.</summary>
    private static List<string> Errors(object request)
    {
        var errors = new List<string>();
        var type = request.GetType();
        foreach (var parameter in type.GetConstructors().Single().GetParameters())
        {
            var value = type.GetProperty(parameter.Name!)!.GetValue(request);
            var context = new ValidationContext(request) { MemberName = parameter.Name };
            var attributes = parameter.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>();
            if (attributes.Any(attribute => attribute.GetValidationResult(value, context) != ValidationResult.Success))
                errors.Add(parameter.Name!);
        }

        if (errors.Count == 0)
            errors.AddRange(((IValidatableObject)request).Validate(new ValidationContext(request)).SelectMany(result => result.MemberNames));
        return errors;
    }
}
