using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("OrderItemModifiers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModifierName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.PriceDelta).HasColumnType("decimal(18,2)");

        builder.HasOne<ModifierOption>()
            .WithMany()
            .HasForeignKey(m => m.ModifierOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
