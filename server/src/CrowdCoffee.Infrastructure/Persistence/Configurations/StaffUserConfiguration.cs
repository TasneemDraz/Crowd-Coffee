using CrowdCoffee.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.ToTable("StaffUsers");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.IsActive).IsRequired();
    }
}
