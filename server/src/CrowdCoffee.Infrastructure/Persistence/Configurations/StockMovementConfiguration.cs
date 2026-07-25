using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.StockItemId, m.CreatedAt });

        builder.Property(m => m.Quantity).HasColumnType("decimal(18,3)");
        builder.Property(m => m.MovementType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.Property(m => m.CreatedBy).IsRequired().HasMaxLength(100);

        builder.HasOne<StockItem>()
            .WithMany()
            .HasForeignKey(m => m.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
