using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("ModifierGroups");
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.Slug).IsUnique();

        builder.Property(g => g.Slug).IsRequired().HasMaxLength(50);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.SelectionType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(g => g.MaxSelections).IsRequired();
        builder.Property(g => g.IsRequired).IsRequired();
        builder.Property(g => g.DisplayOrder).IsRequired();

        builder.HasMany(g => g.Options)
            .WithOne()
            .HasForeignKey(o => o.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(g => g.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
