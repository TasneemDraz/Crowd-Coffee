using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("ProductModifierGroups");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CategorySlug, x.ModifierGroupId }).IsUnique();

        builder.Property(x => x.CategorySlug).IsRequired().HasMaxLength(50);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategorySlug)
            .HasPrincipalKey(c => c.Slug)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ModifierGroup>()
            .WithMany()
            .HasForeignKey(x => x.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
