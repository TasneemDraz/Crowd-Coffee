using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class ModifierOptionConfiguration : IEntityTypeConfiguration<ModifierOption>
{
    public void Configure(EntityTypeBuilder<ModifierOption> builder)
    {
        builder.ToTable("ModifierOptions");
        builder.HasKey(o => o.Id);

        // Slugs collide across groups by design (e.g. "vanilla" appears as both a sugar-free
        // and a regular syrup at different price points, per §4a) — unique per group, not globally.
        builder.HasIndex(o => new { o.ModifierGroupId, o.Slug }).IsUnique();

        builder.Property(o => o.Slug).IsRequired().HasMaxLength(50);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(100);
        builder.Property(o => o.PriceDelta).HasColumnType("decimal(18,2)");
        builder.Property(o => o.IsActive).IsRequired();
        builder.Property(o => o.DisplayOrder).IsRequired();
    }
}
