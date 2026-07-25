using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.IdempotencyKey).IsUnique();

        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(20);
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Phone).IsRequired().HasMaxLength(30);
        builder.Property(o => o.Mode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Address).HasMaxLength(500);
        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.DeliveryFee).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Total).HasColumnType("decimal(18,2)");
        builder.Property(o => o.PaymentMethod).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(2000);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
