using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Unit).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.QuantityOnHand).HasColumnType("decimal(18,3)");
        builder.Property(s => s.QuantityReserved).HasColumnType("decimal(18,3)");
        builder.Property(s => s.ReorderPoint).HasColumnType("decimal(18,3)");
        builder.Property(s => s.IsActive).IsRequired();

        builder.Ignore(s => s.Available);
    }
}
