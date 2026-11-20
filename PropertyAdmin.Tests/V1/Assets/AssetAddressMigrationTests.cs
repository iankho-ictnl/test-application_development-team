using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropertyAdmin.Data;

namespace PropertyAdmin.Tests.V1.Assets;

/// <summary>
/// T-138 (tester), TC-REQ3-5 (REQ-003, D-041): the asset migration keeps the old address as the street and adds
/// house number and country. The PostgreSQL script is generated with the Npgsql provider without a database
/// connection, as in OwnerAddressMigrationTests.
/// </summary>
public sealed class AssetAddressMigrationTests
{
    private const string Previous = "20261120140829_OwnerAddressFields";
    private const string Target = "20261120155801_AssetAddressFields";

    private static string Script()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=not-used;Database=not-used").Options);
        return context.GetService<IMigrator>().GenerateScript(Previous, Target);
    }

    [Fact]
    public void Migration_renames_Address_to_Street_and_never_drops_it()
    {
        var sql = Script();

        Assert.Contains("ALTER TABLE \"Assets\" RENAME COLUMN \"Address\" TO \"Street\";", sql);
        Assert.DoesNotContain("DROP COLUMN", sql);
    }

    [Fact]
    public void Migration_adds_house_number_and_country_as_required_columns()
    {
        var sql = Script();

        Assert.Contains("ALTER TABLE \"Assets\" ADD \"Country\" character varying(100) NOT NULL", sql);
        Assert.Contains("ALTER TABLE \"Assets\" ADD \"HouseNumber\" character varying(20) NOT NULL", sql);
    }
}
