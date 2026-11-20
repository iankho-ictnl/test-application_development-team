using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PropertyAdmin.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<VatRate> VatRates => Set<VatRate>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().HasOne(u => u.Owner).WithMany().HasForeignKey(u => u.OwnerId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Owner>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Street).HasMaxLength(200).IsRequired().HasDefaultValue("");
            e.Property(x => x.HouseNumber).HasMaxLength(20).IsRequired().HasDefaultValue("");
            e.Property(x => x.PostalCode).HasMaxLength(20).IsRequired().HasDefaultValue("");
            e.Property(x => x.City).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(x => x.Country).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(x => x.KvkNumber).HasMaxLength(8);
            e.Property(x => x.VatNumber).HasMaxLength(14);
        });

        builder.Entity<Asset>(e =>
        {
            e.HasOne(x => x.Owner).WithMany(o => o.Assets).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(200).IsRequired();
            e.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        });

        builder.Entity<VatRate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Percentage).HasPrecision(5, 2);
            e.HasIndex(x => new { x.Name, x.ValidFrom }).IsUnique();
        });

        builder.Entity<Expense>(e =>
        {
            e.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.VatRateName).HasMaxLength(50);
            e.Property(x => x.AmountExclVat).HasPrecision(18, 2);
            e.Property(x => x.VatPercentage).HasPrecision(5, 2);
            e.HasIndex(x => new { x.AssetId, x.Date });
        });

        builder.Entity<Activity>(e =>
        {
            e.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.VatRateName).HasMaxLength(50);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PricingType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Hours).HasPrecision(9, 2);
            e.Property(x => x.HourlyRate).HasPrecision(18, 2);
            e.Property(x => x.FixedPrice).HasPrecision(18, 2);
            e.Property(x => x.AmountExclVat).HasPrecision(18, 2);
            e.Property(x => x.VatPercentage).HasPrecision(5, 2);
            e.HasIndex(x => new { x.AssetId, x.Date });
        });

        builder.Entity<Invoice>(e =>
        {
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Number).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        builder.Entity<InvoiceLine>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(600).IsRequired();
            e.Property(x => x.AmountExclVat).HasPrecision(18, 2);
            e.Property(x => x.VatPercentage).HasPrecision(5, 2);
            e.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(20);
        });
    }
}
