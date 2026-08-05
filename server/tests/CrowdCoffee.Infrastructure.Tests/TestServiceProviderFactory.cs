using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Persistence;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using CrowdCoffee.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CrowdCoffee.Infrastructure.Tests;

/// <summary>
/// Points at the same local SQL Server dev database as `dotnet run` (no separate test DB exists
/// yet at this stage of the project) — tests are responsible for cleaning up whatever rows they
/// insert. Wires the same DI graph as Program.cs for the inventory slice, so these tests exercise
/// the real IDomainEventDispatcher pipeline, not a hand-rolled substitute.
/// </summary>
public static class TestServiceProviderFactory
{
    private const string ConnectionString = "Server=.;Database=CrowdCoffee;Trusted_Connection=True;TrustServerCertificate=True";

    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, ReleaseStockOnOrderCancelledHandler>();
        services.AddScoped<IDomainEventHandler<OrderCompletedEvent>, FulfillStockOnOrderCompletedHandler>();

        services.AddDbContext<CrowdCoffeeDbContext>(options => options.UseSqlServer(ConnectionString));

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IOrderStockRequirementRepository, OrderStockRequirementRepository>();
        services.AddScoped<StockReservationService>();

        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        services.AddMemoryCache();
        services.AddSingleton<ICatalogCache, MemoryCatalogCache>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<GetProductModifiersHandler>();

        services.AddScoped<RepriceCartHandler>();

        return services.BuildServiceProvider();
    }
}
