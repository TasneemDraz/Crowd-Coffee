using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Modifiers)
            .WithOne()
            .HasForeignKey(m => m.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Modifiers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
