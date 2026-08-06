using CrowdCoffee.Application.Common;
using CrowdCoffee.Domain.Catalog;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Domain.Staff;
using CrowdCoffee.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace CrowdCoffee.Infrastructure.Persistence;

public class CrowdCoffeeDbContext(DbContextOptions<CrowdCoffeeDbContext> options, IDomainEventDispatcher domainEventDispatcher) : DbContext(options)
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

    public DbSet<OrderNumberCounter> OrderNumberCounters => Set<OrderNumberCounter>();

    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrowdCoffeeDbContext).Assembly);

        modelBuilder.Entity<Category>().HasData(MenuSeedData.Categories);
        modelBuilder.Entity<Product>().HasData(MenuSeedData.Products);
        modelBuilder.Entity<ModifierGroup>().HasData(MenuSeedData.ModifierGroups);
        modelBuilder.Entity<ModifierOption>().HasData(MenuSeedData.ModifierOptions);
        modelBuilder.Entity<ProductModifierGroup>().HasData(MenuSeedData.ProductModifierGroups);
    }

    /// Collects domain events raised by tracked aggregates (e.g. Order.TransitionTo(Cancelled))
    /// and dispatches them only after the save actually succeeds — a handler reacting to an
    /// event that got rolled back would be reacting to something that never happened.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FixUpNewAppendOnlyChildren();

        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }

    /// <summary>
    /// Order.TransitionTo appends a new OrderStatusHistory to a collection navigation that's
    /// usually not loaded (callers only need to append, not read history), and its Id is a
    /// client-generated Guid. EF's fixup can't distinguish "brand new" from "existing row nobody
    /// queried yet" from a non-default key alone, so it tracks the new row as Modified and emits
    /// an UPDATE against a PK that was never inserted — a DbUpdateConcurrencyException with 0
    /// rows affected. OrderStatusHistory is append-only by design (no mutator methods exist on
    /// it), so a Modified entry can only ever be this misclassification, never a real edit —
    /// safe to force back to Added.
    /// </summary>
    private void FixUpNewAppendOnlyChildren()
    {
        foreach (var entry in ChangeTracker.Entries<OrderStatusHistory>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }
    }
}
