using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("PaymentEvents");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.GatewayTransactionId).IsUnique();
        builder.Property(e => e.GatewayTransactionId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.AmountEgp).HasColumnType("decimal(18,2)");
    }
}
