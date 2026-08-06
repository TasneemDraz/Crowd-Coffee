using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class OrderNumberCounterConfiguration : IEntityTypeConfiguration<OrderNumberCounter>
{
    public void Configure(EntityTypeBuilder<OrderNumberCounter> builder)
    {
        builder.ToTable("OrderNumberCounters");
        builder.HasKey(c => c.DateKey);
        builder.Property(c => c.DateKey).HasMaxLength(6).IsUnicode(false);
        builder.Property(c => c.LastValue).IsRequired();
    }
}
