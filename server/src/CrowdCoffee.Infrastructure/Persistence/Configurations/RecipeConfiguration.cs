using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrowdCoffee.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes", t => t.HasCheckConstraint(
            "CK_Recipes_ExactlyOneOwner",
            "([ProductId] IS NOT NULL AND [ModifierOptionId] IS NULL) OR ([ProductId] IS NULL AND [ModifierOptionId] IS NOT NULL)"));
        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuantityPerUnit).HasColumnType("decimal(18,3)");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ModifierOption>()
            .WithMany()
            .HasForeignKey(r => r.ModifierOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StockItem>()
            .WithMany()
            .HasForeignKey(r => r.StockItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
