using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Domain.Staff;
using CrowdCoffee.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence;

public class CrowdCoffeeDbContext(DbContextOptions<CrowdCoffeeDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<ModifierOption> ModifierOptions => Set<ModifierOption>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemModifier> OrderItemModifiers => Set<OrderItemModifier>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrowdCoffeeDbContext).Assembly);

        modelBuilder.Entity<Category>().HasData(MenuSeedData.Categories);
        modelBuilder.Entity<Product>().HasData(MenuSeedData.Products);
        modelBuilder.Entity<ModifierGroup>().HasData(MenuSeedData.ModifierGroups);
        modelBuilder.Entity<ModifierOption>().HasData(MenuSeedData.ModifierOptions);
        modelBuilder.Entity<ProductModifierGroup>().HasData(MenuSeedData.ProductModifierGroups);
    }
}
