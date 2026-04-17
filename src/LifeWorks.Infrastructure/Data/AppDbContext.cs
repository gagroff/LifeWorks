using LifeWorks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeWorks.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Contractor> Contractors => Set<Contractor>();
    public DbSet<HomeImprovement> HomeImprovements => Set<HomeImprovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Contractor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CompanyName).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Website).HasMaxLength(500);
        });

        modelBuilder.Entity<HomeImprovement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Cost).HasPrecision(18, 2);

            e.HasOne(x => x.Property)
                .WithMany(x => x.HomeImprovements)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
                .WithMany(x => x.HomeImprovements)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Contractor)
                .WithMany(x => x.HomeImprovements)
                .HasForeignKey(x => x.ContractorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>().HasData(
            new Property { Id = new Guid("11111111-0000-0000-0000-000000000001"), Name = "Primary Home", Address = string.Empty, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Property { Id = new Guid("11111111-0000-0000-0000-000000000002"), Name = "Lake House", Address = string.Empty, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        var categories = new[]
        {
            ("Appliances",         1),
            ("Electrical",         2),
            ("Exterior / Siding",  3),
            ("Flooring",           4),
            ("HVAC",               5),
            ("Landscaping",        6),
            ("Painting",           7),
            ("Plumbing",           8),
            ("Roofing",            9),
            ("Structural",        10),
            ("Windows & Doors",   11),
            ("Other",             12),
        };

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int index = 1;
        modelBuilder.Entity<Category>().HasData(
            categories.Select(c => new Category
            {
                Id = new Guid($"22222222-0000-0000-0000-{index++:D12}"),
                Name = c.Item1,
                SortOrder = c.Item2,
                IsSeeded = true
            }).ToArray()
        );
    }
}
