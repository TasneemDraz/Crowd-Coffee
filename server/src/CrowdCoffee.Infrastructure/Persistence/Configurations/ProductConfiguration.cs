using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.CategorySlug).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.IsAvailable).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.DisplayOrder).IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategorySlug)
            .HasPrincipalKey(c => c.Slug)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
