using CrowdCoffee.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistories");
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => h.OrderId);

        builder.Property(h => h.FromStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ChangedBy).IsRequired().HasMaxLength(100);
        builder.Property(h => h.Reason).HasMaxLength(500);
    }
}
