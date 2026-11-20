using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PropertyAdmin.Data;

namespace PropertyAdmin.Tests.V1.Owners;

/// <summary>
/// T-104 (tester), TC-REQ2-5 (REQ-002, D-041): the owner migration keeps the old address as the street.
/// The PostgreSQL script is generated with the Npgsql provider without a database connection; the run
/// against a real PostgreSQL 18 (D-031, Testcontainers) is not in the test project yet.
/// </summary>
public sealed class OwnerAddressMigrationTests
{
    private const string Previous = "20261111075401_OwnerInactiveSince";
    private const string Target = "20261120140829_OwnerAddressFields";

    private static AppDbContext NpgsqlContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=not-used;Database=not-used").Options);

    private static string Script()
    {
        using var context = NpgsqlContext();
        return context.GetService<IMigrator>().GenerateScript(Previous, Target);
    }

    [Fact]
    public void Migration_renames_Address_to_Street_and_never_drops_it()
    {
        var sql = Script();

        Assert.Contains("ALTER TABLE \"Owners\" RENAME COLUMN \"Address\" TO \"Street\";", sql);
        Assert.DoesNotContain("DROP COLUMN \"Address\"", sql);
        Assert.DoesNotContain("DROP COLUMN \"Street\"", sql);
    }

    [Fact]
    public void Migration_fills_nulls_before_NOT_NULL_and_adds_the_new_columns()
    {
        var sql = Script();

        var coalesce = sql.IndexOf("COALESCE(\"Address\", '')", StringComparison.Ordinal);
        var rename = sql.IndexOf("RENAME COLUMN \"Address\" TO \"Street\"", StringComparison.Ordinal);
        var notNull = sql.IndexOf("ALTER COLUMN \"Street\" SET NOT NULL", StringComparison.Ordinal);
        Assert.True(coalesce >= 0 && rename > coalesce && notNull > rename, sql);
        foreach (var column in new[] { "Country", "HouseNumber", "KvkNumber", "VatNumber" })
            Assert.Contains($"ADD \"{column}\" ", sql);
    }

    [Fact]
    public void Model_has_no_changes_that_are_missing_from_the_migrations()
    {
        using var context = NpgsqlContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }
}
