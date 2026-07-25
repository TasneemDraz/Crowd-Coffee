using CrowdCoffee.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.HasAlternateKey(c => c.Slug);

        builder.Property(c => c.Slug).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Label).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.DisplayOrder).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
    }
}
